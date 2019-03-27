import { BotFrameworkAdapter, TurnContext, WebRequest, WebResponse } from "botbuilder";
import { Activity, ConversationReference, ResourceResponse } from "botframework-schema";

export class SkillAdapter extends BotFrameworkAdapter {
    private readonly queuedActivities: Partial<Activity>[];

    constructor() {
        super();
        this.queuedActivities = [];
    }

    public sendActivities(context: TurnContext, activities: Partial<Activity>[]): Promise<ResourceResponse[]> {
        throw new Error("Method not implemented.");
    }

    public continueConversation(reference: Partial<ConversationReference>, logic: (revocableContext: TurnContext) => Promise<void>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public processActivity(req: WebRequest, res: WebResponse, logic: (context: TurnContext) => Promise<any>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public updateActivity(context: TurnContext, activity: Partial<Activity>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public deleteActivity(context: TurnContext, reference: Partial<ConversationReference>): Promise<void> {
        throw new Error("Method not implemented.");
    }
}
