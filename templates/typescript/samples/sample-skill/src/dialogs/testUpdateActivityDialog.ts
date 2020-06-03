/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity, BotTelemetryClient, MessageFactory,
    RecognizerResult, ResourceResponse, StatePropertyAccessor, TurnContext, Attachment
} from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import { Dialog, DialogContext, DialogInstance, DialogTurnResult } from 'botbuilder-dialogs';
import { Card, LocaleTemplateManager } from 'bot-solutions';
import i18next from 'i18next';
import { SkillState } from '../models/skillState';
import { BotServices } from '../services/botServices';
import { IBotSettings } from '../services/botSettings';
import { SkillDialogBase } from './skillDialogBase';


export class TestUpdateActivityDialog extends SkillDialogBase {

    protected readonly stateAccessor: StatePropertyAccessor<SkillState>;
    protected readonly templateEngine: LocaleTemplateManager;
    private static readonly CARD_ACTIVITY_IDENTIFIER: string = 'testUpdateCard';

    // Constructor
    public constructor(
        settings: Partial<IBotSettings>,
        services: BotServices,
        templateEngine: LocaleTemplateManager,
        stateAccessor: StatePropertyAccessor<SkillState>,
        telemetryClient: BotTelemetryClient
    ) {
        super(TestUpdateActivityDialog.name, settings, services,stateAccessor, telemetryClient, templateEngine);
        this.stateAccessor = stateAccessor;
        this.templateEngine = templateEngine;
        this.initialDialogId = TestUpdateActivityDialog.name;
    }

    //Begin Dialog method
    public async beginDialog(dc: DialogContext, options?: {} | undefined): Promise<DialogTurnResult> {
        const skillState: SkillState = await this.stateAccessor.get(dc.context, new SkillState());
        skillState.cardsToUpdate = {};
        const act: Partial<Activity> = this.templateManager.generateActivityForLocale('TestCard',{"name":"Send Activity"});
        this.registerActivityListener(dc);
        if(act.attachments)
            await this.sendOrUpdateCard(dc,act.attachments[0], TestUpdateActivityDialog.CARD_ACTIVITY_IDENTIFIER)
        else
            await dc.context.sendActivity(this.templateManager.generateActivityForLocale('UnsupportedMessage'));
        return Dialog.EndOfTurn;
    }

    //Performing some action
    public async continueDialog(dc: DialogContext): Promise<DialogTurnResult> {

        const act: Partial<Activity> = this.templateManager.generateActivityForLocale('TestCard',{"name":"Update Activity"});
        if(act.attachments)
            await this.sendOrUpdateCard(dc,act.attachments[0], TestUpdateActivityDialog.CARD_ACTIVITY_IDENTIFIER)
        else
            await dc.context.sendActivity(this.templateManager.generateActivityForLocale('UnsupportedMessage'));

        return Dialog.EndOfTurn;
    }

    /**
     * Send / Update a card with a given name
     * @param card the actual card to send/update
     * @param activityIdentifier name of the card to be sent, used to look up its corresponding activity to see if it was sent before
     * @param forceSend whether to force the send of a new card even if a card with the same name was sent before (i.e do not update)
     */
    private async sendOrUpdateCard(
        dc: DialogContext,
        card: Attachment, activityIdentifier: string): Promise<void> {
        
        const skillState: SkillState = await this.stateAccessor.get(dc.context, new SkillState());

        let previouslySentActivity: Partial<Activity> | undefined = skillState.cardsToUpdate[activityIdentifier];

        if (previouslySentActivity === undefined || dc.context.activity.channelId !== 'msteams') {
            // send a new card and set the activityName so that our listener knows that this is an activity we want to keep
            const responseToUser: Partial<Activity> = MessageFactory.attachment(card);
            responseToUser.channelData = {
                activityName: activityIdentifier
            };
            const cardResponse: ResourceResponse | undefined = await dc.context.sendActivity(responseToUser);

            // the previouslySentActivity should now have been filled by the onSendActivities listener
            previouslySentActivity = skillState.cardsToUpdate[activityIdentifier];

            // store the activity id, which we cannot get in the listener which we register in beginDialog
            if (cardResponse && previouslySentActivity) {
                previouslySentActivity.id = cardResponse.id;
            }
        } else {
            previouslySentActivity.attachments = [card];
            await dc.context.updateActivity(previouslySentActivity);
        }
    }

    private registerActivityListener(dc: DialogContext): void {
        // listen to the activities being sent and save them so we can update them later
        // for updating an activity, we need:
        // - the activity id
        // - the conversation id
        // - the service URL
        // which we unfortunately cannot get all just from the response to the sendActivity, so we must use a listener here
        // we cannot get the activity id from this listener however, so we have to get it from the response
        dc.context.onSendActivities(async (
            context: TurnContext, activities: Partial<Activity>[], nextSend: () => Promise<ResourceResponse[]>) => {
            activities.forEach(async (activity: Partial<Activity>) => {
                const state: SkillState = <SkillState>await this.stateAccessor.get(dc.context);
                if (activity.channelData && activity.channelData.activityName) {
                    state.cardsToUpdate[activity.channelData.activityName] = activity;
                }
            });

            return nextSend();
        });
    }

}
