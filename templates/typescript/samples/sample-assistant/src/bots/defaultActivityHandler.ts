/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    ActivityTypes,
    BotState,
    Channels,
    ConversationState,
    SigninStateVerificationQuery,
    StatePropertyAccessor,
    TeamsActivityHandler,
    TurnContext, 
    UserState,
    BotTelemetryClient } from 'botbuilder';
import {
    Dialog,
    DialogSet,
    DialogState,
    runDialog } from 'botbuilder-dialogs';
import { LocaleTemplateManager, TokenEvents } from 'bot-solutions';
import { IUserProfileState } from '../models/userProfileState';

export class DefaultActivityHandler<T extends Dialog> extends TeamsActivityHandler {

    private readonly dialog: Dialog;
    private readonly dialogs: DialogSet;
    private readonly conversationState: BotState;
    private readonly userState: BotState;
    private readonly dialogStateAccessor: StatePropertyAccessor<DialogState>;
    private readonly userProfileState: StatePropertyAccessor<IUserProfileState>;
    private readonly templateManager: LocaleTemplateManager;

    public constructor(
        conversationState: ConversationState,
        userState: UserState,
        templateManager: LocaleTemplateManager,
        dialog: T,
        telemetryClient: BotTelemetryClient
    ) {
        super();
        this.dialog = dialog;
        this.dialog.telemetryClient = telemetryClient;
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
        this.userProfileState = userState.createProperty<IUserProfileState>('UserProfileState');
        this.templateManager = templateManager;
        this.dialogs = new DialogSet(this.dialogStateAccessor);
        this.dialogs.add(this.dialog);
        super.onMembersAdded(this.membersAdded.bind(this));
    }

    public async onTurnActivity(turnContext: TurnContext): Promise<void> {
        await super.onTurnActivity(turnContext);

        // Save any state changes that might have occured during the turn.
        await this.conversationState.saveChanges(turnContext, false);
        await this.userState.saveChanges(turnContext, false);
    }

    protected async membersAdded(turnContext: TurnContext): Promise<void> {
        const userProfile: IUserProfileState = await this.userProfileState.get(turnContext, () => { name: ''; });

        if (userProfile.name === undefined || userProfile.name.trim().length === 0) {
            // Send new user intro card.
            await turnContext.sendActivity(this.templateManager.generateActivityForLocale('NewUserIntroCard', turnContext.activity.locale, userProfile));
        } else {
            // Send returning user intro card.
            await turnContext.sendActivity(this.templateManager.generateActivityForLocale('ReturningUserIntroCard', turnContext.activity.locale, userProfile));
        }
        
        await runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onMessageActivity(turnContext: TurnContext): Promise<void> {
        // directline speech occasionally sends empty message activities that should be ignored
        const activity: Activity = turnContext.activity;
        if (activity.channelId === Channels.DirectlineSpeech &&
            activity.type === ActivityTypes.Message &&
            (activity.text === undefined || activity.text.trim().length === 0)) {
            return Promise.resolve();
        }
        return runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async handleTeamsSigninVerifyState(turnContext: TurnContext, query: SigninStateVerificationQuery): Promise<void> {
        return runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onEventActivity(turnContext: TurnContext): Promise<void> {
        //PENDING: This should be const ev: IEventActivity = innerDc.context.activity.asEventActivity()
        // but it's not in botbuilder-js currently
        const ev: Activity = turnContext.activity;

        switch (ev.name) {
            case TokenEvents.tokenResponseEventName:
                // Forward the token response activity to the dialog waiting on the stack.
                await runDialog(this.dialog, turnContext, this.dialogStateAccessor);
                break;
            default:
                await turnContext.sendActivity({ type: ActivityTypes.Trace, text: `Unknown Event '${ ev.name ?? 'undefined' }' was received but not processed.` });
                break;
        }
    }

    protected async onEndOfConversationActivity(turnContext: TurnContext): Promise<void>{
        await runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }
}
