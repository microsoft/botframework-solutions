import { BotAdapter, BotHandler, InvokeResponse, TurnContext } from 'botbuilder';
import { IRemoteUserTokenProvider } from 'botbuilder-solutions';
import { Activity, ConversationReference } from 'botframework-schema';
import { IActivityHandler } from '../activityHandler';

export class SkillHttpBotAdapter extends BotAdapter implements IActivityHandler, IRemoteUserTokenProvider {
    constructor() {
        super();
    }

    public async sendActivities(context: TurnContext, activities: Partial<Activity>[]): Promise<import("botframework-schema").ResourceResponse[]> {
        throw new Error("Method not implemented.");
    }

    public async deleteActivity(context: TurnContext, reference: Partial<ConversationReference>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public async updateActivity(context: TurnContext, activity: Partial<Activity>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public async continueConversation(reference: Partial<ConversationReference>, logic: (revocableContext: TurnContext) => Promise<void>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public async processActivity(activity: Activity, callback: BotHandler): Promise<InvokeResponse> {
        throw new Error("Method not implemented.");
    }
    
    public async sendRemoteTokenRequestEvent(turnContext: TurnContext): Promise<void> {
        throw new Error("Method not implemented.");
    }
}
