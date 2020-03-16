/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotFrameworkAdapter,
    BotTelemetryClient,
    RecognizerResult,
    StatePropertyAccessor,
    TurnContext
} from 'botbuilder';
import { LuisRecognizer, QnAMakerResult, QnAMaker } from 'botbuilder-ai';
import {
    DialogContext,
    DialogTurnResult,
    Dialog
} from 'botbuilder-dialogs';
import {
    ActivityHandlerDialog,
    DialogContextEx,
    ICognitiveModelSet,
    InterruptionAction,
    ISkillManifest,
    LocaleTemplateEngineManager,
    SkillContext,
    SkillDialog,
    SkillRouter,
    SwitchSkillDialog,
    SwitchSkillDialogOptions,
    TokenEvents
} from 'botbuilder-solutions';
import { TokenStatus } from 'botframework-connector';
import { Activity, ActivityTypes, ResourceResponse } from 'botframework-schema';
import { IUserProfileState } from '../models/userProfileState';
import { BotServices } from '../services/botServices';
import { IBotSettings } from '../services/botSettings';
import { OnboardingDialog } from './onboardingDialog';

enum Events {
    location = 'VA.Location',
    timeZone = 'VA.Timezone'
}

enum StateProperties {
    dispatchResult = 'dispatchResult',
    generalResult = 'generalResult',
    previousBotResponse = 'previousBotResponse',
    location = 'location',
    timeZone = 'timezone'
}

// Dialog providing activity routing and message/event processing.
export class MainDialog extends ActivityHandlerDialog {
    private readonly services: BotServices;
    private readonly settings: IBotSettings;
    private onboardingDialog: OnboardingDialog;
    private switchSkillDialog: SwitchSkillDialog;
    private templateEngine: LocaleTemplateEngineManager;
    private readonly skillContext: StatePropertyAccessor<SkillContext>;
    private userProfileState: StatePropertyAccessor<IUserProfileState>;
    private previousResponseAccesor: StatePropertyAccessor<Partial<Activity>[]>

    public constructor(
        settings: IBotSettings,
        services: BotServices,
        templateEngine: LocaleTemplateEngineManager,
        userProfileState: StatePropertyAccessor<IUserProfileState>,
        skillContext: StatePropertyAccessor<SkillContext>,
        previousResponseAccessor: StatePropertyAccessor<Partial<Activity>[]>,
        onboardingDialog: OnboardingDialog,
        switchSkillDialog: SwitchSkillDialog,
        skillDialogs: SkillDialog[],
        telemetryClient: BotTelemetryClient
    ) {
        super(MainDialog.name, telemetryClient);

        this.services = services;
        this.settings = settings;
        this.templateEngine = templateEngine;
        this.telemetryClient = telemetryClient;

        // Create user state properties
        this.userProfileState = userProfileState;
        this.skillContext = skillContext;

        // Create conversation state properties
        this.previousResponseAccesor = previousResponseAccessor;

        // Register dialogs
        this.onboardingDialog = onboardingDialog;
        this.switchSkillDialog = switchSkillDialog;
        this.addDialog(this.onboardingDialog);
        this.addDialog(this.switchSkillDialog);

        skillDialogs.forEach((skillDialog: SkillDialog): void => {
            this.addDialog(skillDialog);
        });
    }

    // Runs on every turn of the conversation.
    protected async onContinueDialog(innerDc: DialogContext): Promise<DialogTurnResult> {
        if (innerDc.context.activity.type == ActivityTypes.Message) {
            // Get cognitive models for the current locale.
            const localizedServices = this.services.getCognitiveModels();

            // Run LUIS recognition and store result in turn state.
            const dispatchResult: RecognizerResult = await localizedServices.dispatchService.recognize(innerDc.context);
            innerDc.context.turnState.set(StateProperties.dispatchResult, dispatchResult);

            const intent: string = LuisRecognizer.topIntent(dispatchResult);
            if (intent == 'l_general') {
                // Run LUIS recognition on General model and store result in turn state.
                const generalLuis: LuisRecognizer | undefined = localizedServices.luisServices.get('general');
                if (generalLuis !== undefined) {
                    const generalResult: RecognizerResult = await generalLuis.recognize(innerDc.context);
                    innerDc.context.turnState.set(StateProperties.generalResult, generalResult);
                }
            }
        }

        // Set up response caching for "repeat" functionality.
        innerDc.context.onSendActivities(this.storeOutgoingActivities.bind(this));

        return await super.onContinueDialog(innerDc);
    }

    // Runs on every turn of the conversation to check if the conversation should be interrupted.
    protected async onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {
        const activity: Activity = dc.context.activity;
        const userProfile: IUserProfileState = await this.userProfileState.get(dc.context, { name: '' });
        const dialog: Dialog | undefined = dc.activeDialog !== undefined ? dc.findDialog(dc.activeDialog.id) : undefined;

        if (activity.type === ActivityTypes.Message && activity.text.trim().length > 0) {
            // Check if the active dialog is a skill for conditional interruption.
            const isSkill: boolean = dialog instanceof SkillDialog;

            // Get Dispatch LUIS result from turn state.
            const dispatchResult: RecognizerResult = dc.context.turnState.get(StateProperties.dispatchResult);
            const intent: string = LuisRecognizer.topIntent(dispatchResult);

            // Check if we need to switch skills.
            if (isSkill) {
                if (dialog !== undefined) {
                    if (intent !== dialog.id && dispatchResult.intents[intent].score > 0.9) {
                        const identifiedSkill: ISkillManifest | undefined = SkillRouter.isSkill(this.settings.skills, LuisRecognizer.topIntent(dispatchResult).toString());

                        if (identifiedSkill) {
                            const prompt: Partial<Activity> = this.templateEngine.generateActivityForLocale('SkillSwitchPrompt', { skill: identifiedSkill.name });
                            await dc.beginDialog(this.switchSkillDialog.id, new SwitchSkillDialogOptions(prompt as Activity, identifiedSkill));

                            return InterruptionAction.Waiting;
                        }
                    }
                }
            }

            if (intent == 'l_general') {
                // Get connected LUIS result from turn state.
                const generalResult: RecognizerResult = dc.context.turnState.get(StateProperties.generalResult);
                const intent: string = LuisRecognizer.topIntent(generalResult);

                if (generalResult.intents[intent].score > 0.5) {
                    switch (intent.toString()) {
                        case 'Cancel': {
                            // Suppress completion message for utility functions.
                            DialogContextEx.suppressCompletionMessage(dc, true);

                            await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('CancelledMessage', userProfile));
                            await dc.cancelAllDialogs();

                            return InterruptionAction.End;
                        }

                        case 'Escalate': {
                            await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('EscalateMessage', userProfile));
                            return InterruptionAction.Resume;
                        }

                        case 'Help': {
                            // Suppress completion message for utility functions.
                            DialogContextEx.suppressCompletionMessage(dc, true);

                            if (isSkill) {
                                // If current dialog is a skill, allow it to handle its own help intent.
                                await dc.continueDialog();
                                break;
                            } else {
                                await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('HelpCard', userProfile));
                                return InterruptionAction.Resume;
                            }
                        }

                        case 'Logout': {
                            // Suppress completion message for utility functions.
                            DialogContextEx.suppressCompletionMessage(dc, true);

                            // Log user out of all accounts.
                            await this.logUserOut(dc);

                            await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('LogoutMessage', userProfile));
                            return InterruptionAction.End;
                        }

                        case 'Repeat': {
                            // No need to send the usual dialog completion message for utility capabilities such as these.
                            DialogContextEx.suppressCompletionMessage(dc, true);

                            // Sends the activities since the last user message again.
                            const previousResponse: Partial<Activity>[] = await this.previousResponseAccesor.get(dc.context, []);

                            previousResponse.forEach(async (response: Partial<Activity>): Promise<void> => {
                                // Reset id of original activity so it can be processed by the channel.
                                response.id = '';
                                await dc.context.sendActivity(response);
                            });

                            return InterruptionAction.Waiting;
                        }

                        case 'StartOver': {
                            // Suppresss completion message for utility functions.
                            DialogContextEx.suppressCompletionMessage(dc, true);

                            await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('StartOverMessage', userProfile));

                            // Cancel all dialogs on the stack.
                            await dc.cancelAllDialogs();

                            return InterruptionAction.End;
                        }

                        case 'Stop': {
                            // Use this intent to send an event to your device that can turn off the microphone in speech scenarios.
                            break;
                        }
                    }
                }
            }
        }

        return InterruptionAction.NoAction;
    }

    // Runs when the dialog stack is empty, and a new member is added to the conversation. Can be used to send an introduction activity.
    protected async onMembersAdded(innerDc: DialogContext): Promise<void> {
        const userProfile: IUserProfileState = await this.userProfileState.get(innerDc.context, { name: '' });

        if (userProfile === undefined || userProfile.name.trim().length === 0) {
            // Send new user intro card.
            await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('NewUserIntroCard', userProfile));

            // Start onboarding dialog.
            await innerDc.beginDialog(OnboardingDialog.name);
        } else {
            // Send returning user intro card.
            await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('ReturningUserIntroCard', userProfile));
        }

        // Suppress completion message.
        DialogContextEx.suppressCompletionMessage(innerDc, true);
    }

    // Runs when the dialog stack is empty, and a new message activity comes in.
    protected async onMessageActivity(innerDc: DialogContext): Promise<void> {
        //PENDING: This should be const activity: IMessageActivity = innerDc.context.activity.asMessageActivity()
        // but it's not in botbuilder-js currently
        const activity: Activity = innerDc.context.activity;
        const userProfile: IUserProfileState = await this.userProfileState.get(innerDc.context, { name: '' });

        if (activity !== undefined && activity.text.trim().length > 0) {
            // Get current cognitive models for the current locale.
            const localizedServices: ICognitiveModelSet = this.services.getCognitiveModels();

            // Get dispatch result from turn state.
            const dispatchResult: RecognizerResult = innerDc.context.turnState.get(StateProperties.dispatchResult);
            const dispatch: string = LuisRecognizer.topIntent(dispatchResult);

            // Check if the dispatch intent maps to a skill.
            const identifiedSkill: ISkillManifest | undefined = SkillRouter.isSkill(this.settings.skills, dispatch);

            if (identifiedSkill !== undefined) {
                // Start the skill dialog.
                await innerDc.beginDialog(identifiedSkill.id);
            } else if (dispatch === 'q_faq') {
                const qnaMaker: QnAMaker | undefined = localizedServices.qnaServices.get('faq');
                if (qnaMaker !== undefined) {
                    await this.callQnaMaker(innerDc, qnaMaker);
                }
            }
            else if (dispatch === 'q_chitchat') {
                DialogContextEx.suppressCompletionMessage(innerDc, true);
                const qnaMaker: QnAMaker | undefined = localizedServices.qnaServices.get('chitchat');
                if (qnaMaker !== undefined) {
                    await this.callQnaMaker(innerDc, qnaMaker);
                }
            } else {
                DialogContextEx.suppressCompletionMessage(innerDc, true);

                await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('UnsupportedMessage', userProfile));
            }
        }
    }

    // Runs when a new event activity comes in.
    protected async onEventActivity(innerDc: DialogContext): Promise<void> {
        //PENDING: This should be const ev: IEventActivity = innerDc.context.activity.asEventActivity()
        // but it's not in botbuilder-js currently
        const ev: Activity = innerDc.context.activity;
        const value: string = ev.value.toString();

        switch (ev.name) {
            case Events.location: {
                const locationObj = { location: '' };
                locationObj.location = value;

                // Store location for use by skills.
                const skillContext: SkillContext = await this.skillContext.get(innerDc.context, new SkillContext());
                skillContext.setObj(StateProperties.location, locationObj);
                await this.skillContext.set(innerDc.context, skillContext);

                break;
            }

            case Events.timeZone: {
                try {
                    const tz: string = new Date().toLocaleString(value);
                    const timeZoneObj: { timezone: string } = { timezone: tz };

                    // Store location for use by skills.
                    const skillContext: SkillContext = await this.skillContext.get(innerDc.context, new SkillContext());
                    skillContext.setObj(StateProperties.timeZone, timeZoneObj);
                    await this.skillContext.set(innerDc.context, skillContext);
                }
                catch (err) {
                    await innerDc.context.sendActivity({ type: ActivityTypes.Trace, text: 'Received time zone could not be parsed. Property not set.' });
                }

                break;
            }

            case TokenEvents.tokenResponseEventName: {
                // Forward the token response activity to the dialog waiting on the stack.
                await innerDc.continueDialog();
                break;
            }

            default: {
                await innerDc.context.sendActivity({ type: ActivityTypes.Trace, text: `Unknown Event 'undefined' was received but not processed.` });
                break;
            }
        }
    }

    // Runs when an activity with an unknown type is received.
    protected async onUnhandledActivityType(innerDc: DialogContext): Promise<void> {
        await innerDc.context.sendActivity({ type: ActivityTypes.Trace, text: 'Unknown activity was received but not processed.' });
    }

    // Runs when the dialog stack completes.
    protected async onDialogComplete(outerDc: DialogContext, result: Record<string, any>): Promise<void> {
        const userProfile: IUserProfileState = await this.userProfileState.get(outerDc.context, { name: '' });

        // Only send a completion message if the user sent a message activity.
        if (outerDc.context.activity.type === ActivityTypes.Message && !DialogContextEx.suppressCompletionMessageValidation(outerDc)) {
            await outerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('CompletedMessage', userProfile));
        }
    }

    private async logUserOut(dc: DialogContext): Promise<void> {
        const tokenProvider: BotFrameworkAdapter = dc.context.adapter as BotFrameworkAdapter;
        if (tokenProvider !== undefined) {
            // Sign out user
            const tokens: TokenStatus[] = await tokenProvider.getTokenStatus(dc.context, dc.context.activity.from.id);
            tokens.forEach(async (token: TokenStatus): Promise<void> => {
                if (token.connectionName !== undefined) {
                    await tokenProvider.signOutUser(dc.context, token.connectionName);
                }
            });

            // Cancel all active dialogs
            await dc.cancelAllDialogs();

        } else {
            throw new Error('OAuthPrompt.SignOutUser(): not supported by the current adapter');
        }
    }

    private async callQnaMaker(innerDc: DialogContext, qnaMaker: QnAMaker): Promise<void> {
        const userProfile: IUserProfileState = await this.userProfileState.get(innerDc.context, { name: '' });

        const answer: QnAMakerResult[] = await qnaMaker.getAnswers(innerDc.context);

        if (answer !== undefined && answer.length > 0) {
            await innerDc.context.sendActivity(answer[0].answer, answer[0].answer);
        } else {
            await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('UnsupportedMessage', userProfile));
        }
    }

    private async storeOutgoingActivities(turnContext: TurnContext, activities: Partial<Activity>[], next: () => Promise<ResourceResponse[]>): Promise<ResourceResponse[]> {
        const messageActivities: Partial<Activity>[] = activities.filter((a: Partial<Activity>): boolean => a.type == ActivityTypes.Message);

        // If the bot is sending message activities to the user (as opposed to trace activities)
        if (messageActivities.length > 0) {
            let botResponse: Partial<Activity>[] = await this.previousResponseAccesor.get(turnContext, []);

            // Get only the activities sent in response to last user message
            botResponse = botResponse.concat(messageActivities)
                .filter((a: Partial<Activity>): boolean => a.replyToId == turnContext.activity.id);

            await this.previousResponseAccesor.set(turnContext, botResponse);
        }

        return await next();
    }
}
