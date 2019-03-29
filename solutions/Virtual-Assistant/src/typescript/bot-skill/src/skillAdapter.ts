import { BotFrameworkAdapter, BotAdapter, TurnContext, WebRequest, WebResponse } from "botbuilder";
import { ActivityExtensions } from '../extensions';
import { Activity, ActivityTypes, ActivityTypesEx, ConversationReference, ResourceResponse } from "botframework-schema";

export class SkillAdapter extends BotFrameworkAdapter {
    private readonly queuedActivities: Partial<Activity>[];
    private lastId: number = 0;

    private get nextId(): string {
        return (this.lastId + 1).toString();
    }

    constructor() {
        super();
        this.queuedActivities = [];
    }

    public processActivity(req: WebRequest, res: WebResponse, logic: (context: TurnContext) => Promise<any>): Promise<void> {
        throw new Error("Method not implemented."); 
    }

    public async sendActivities(context: TurnContext, activities: Partial<Activity>[]): Promise<ResourceResponse[]> {
        const responses: ResourceResponse [] = [];
        const proactiveActivities: Partial<Activity>[] = [];

        activities.forEach(async(activity: Partial<Activity>) => {
            if(!activity.id){
                activity.id = this.nextId;
            }

            if(!activity.timestamp){
                activity.timestamp = new Date();
            }

            if (activity.type === 'delay'){
                // The BotFrameworkAdapter and Console adapter implement this
                // hack directly in the POST method. Replicating that here
                // to keep the behavior as close as possible to facillitate
                // more realistic tests.
                const delayMs: number = activity.value;
                await this.sleep(delayMs);
            }
            else if(activity.type === ActivityTypes.Trace && activity.channelId !== "emulator"){
                // if it is a Trace activity we only send to the channel if it's the emulator.
            }
            else if(activity.type === ActivityTypes.Typing && activity.channelId !== "test"){
               // If it's a typing activity we omit this in test scenarios to avoid test failures
            }
            else{

                //TODO - Post to the Parent Bot ServiceURL
                (this.queuedActivities);
                {
                    this.queuedActivities.push(activity);
                }    
            }

            responses.push( new ResourceResponse (activity.id));
        })    

        return responses;
    }   

    public getReplies(): Partial<Activity>[] {
        return this.queuedActivities
            .splice(0, this.queuedActivities.length)
            .reverse();
    }

    public async continueConversation(reference: Partial<ConversationReference>, logic: (revocableContext: TurnContext) => Promise<void>): Promise<void> {
      
        if (!reference) {
            throw new Error('Missing parameter.  reference is required');
        }

        if (!logic) {
            throw new Error('Missing parameter.  logic is required');
        }

        const context: TurnContext = new TurnContext(this, ActivityExtensions.getContinuationActivity(reference));
        await this.runMiddleware(context, logic);
    }

    private sleep(delay: number): Promise<void> {
        return new Promise<void>((resolve: (value: void) => void): void => {
            setTimeout(resolve, delay);
        });
    }

    public deleteActivity(context: TurnContext, reference: Partial<ConversationReference>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public updateActivity(context: TurnContext, activity: Partial<Activity>): Promise<void> {
        throw new Error("Method not implemented.");
    }
}
