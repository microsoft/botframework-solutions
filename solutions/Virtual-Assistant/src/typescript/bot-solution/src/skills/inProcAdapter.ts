import { Activity, BotAdapter, ConversationReference, Middleware, ResourceResponse, TurnContext } from 'botbuilder';
import { setTimeout } from 'timers';
import { ActivityExtensions } from '../extensions';

export class InProcAdapter extends BotAdapter {
    private readonly queuedActivities: Partial<Activity>[];

    private lastId: number = 0;

    private get nextId(): string {
        return (this.lastId + 1).toString();
    }

    constructor() {
        super();
        this.queuedActivities = [];
    }

    public async processActivity(activity: Partial<Activity>, callback: (revocableContext: TurnContext) => Promise<void>): Promise<void> {
        const context: TurnContext = new TurnContext(this, activity);
        await this.runMiddleware(context, callback);
    }

    public use(middleware: Middleware): this {
        super.use(middleware);

        return this;
    }

    public async continueConversation(reference: Partial<ConversationReference>,
                                      logic: (revocableContext: TurnContext) => Promise<void>): Promise<void> {
        if (!reference) {
            throw new Error('Missing parameter.  reference is required');
        }

        if (!logic) {
            throw new Error('Missing parameter.  logic is required');
        }

        const context: TurnContext = new TurnContext(this, ActivityExtensions.getContinuationActivity(reference));
        await this.runMiddleware(context, logic);
    }

    public getNextReply(): Partial<Activity>|undefined {
        return this.queuedActivities.pop();
    }

    public getReplies(): Partial<Activity>[] {
        return this.queuedActivities
            .splice(0, this.queuedActivities.length)
            .reverse();
    }

    public async sendActivities(context: TurnContext, activities: Partial<Activity>[]): Promise<ResourceResponse[]> {
        const responses: ResourceResponse[] = [];

        activities.forEach(async(activity: Partial<Activity>) => {
            if (!activity.id) {
                activity.id = this.nextId;
            }

            if (!activity.timestamp) {
                activity.timestamp = new Date();
            }

            if (activity.type === 'delay') {
                // The BotFrameworkAdapter and Console adapter implement this
                // hack directly in the POST method. Replicating that here
                // to keep the behavior as close as possible to facilitate
                // more realistic tests.
                const delayMs: number = activity.value;
                await this.sleep(delayMs);
            } else {
                this.queuedActivities.push(activity);
            }

            responses.push({ id: activity.id });
        });

        return responses;
    }

    private sleep(delay: number): Promise<void> {
        return new Promise<void>((resolve: (value: void) => void): void => {
            setTimeout(resolve, delay);
        });
    }

    public updateActivity(context: TurnContext, activity: Partial<Activity>): Promise<void> {
        throw new Error('Method not used.');
    }

    public deleteActivity(context: TurnContext, reference: Partial<ConversationReference>): Promise<void> {
        throw new Error('Method not used.');
    }
}
