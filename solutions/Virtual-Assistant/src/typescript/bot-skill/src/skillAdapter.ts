import { BotFrameworkAdapter, TurnContext, WebRequest, WebResponse, InvokeResponse } from "botbuilder";
import { DialogContext } from "botbuilder-dialogs";
import { ActivityExtensions } from 'bot-solution';
import { Activity, ActivityTypes, ConversationReference, ResourceResponse } from "botframework-schema";

export class SkillAdapter extends BotFrameworkAdapter {
    private readonly queuedActivities: Partial<Activity>[];
    private lastId: number = 0;

    private get nextId(): string {
        return (this.lastId + 1).toString();
    }

    public static isSkillMode(context: TurnContext|DialogContext): boolean {
        const ctx: TurnContext = context instanceof DialogContext ? context.context : context;
        return ctx.turnState.get('skillMode') || false;
    }

    public constructor() {
        super();
        this.queuedActivities = [];
    }

    public async processActivity(req: WebRequest, res: WebResponse, logic: (context: TurnContext) => Promise<any>): Promise<void> {
        // deserialize the incoming Activity
        const activity: Activity = await parseRequest(req);

        // grab the auth header from the inbound http request
        const headers = req.headers;

        // process the inbound activity with the bot
        const invokeResponse: InvokeResponse = await this.processActivityInternal(headers, activity, logic);

        // write the response, potentially serializing the InvokeResponse
        res.status(invokeResponse.status);
        if (invokeResponse.body) {
            res.send(invokeResponse.body);
        }

        res.end();
    }

    public async processActivityInternal(authHeader: string , activity: Partial<Activity>, callback: (revocableContext: TurnContext) => Promise<void>): Promise<InvokeResponse> {
        // Ensure the Activity has been retrieved from the HTTP POST
        // Not performing authentication checks at this time

        // Process the Activity through the Middleware and the Bot, this will generate Activities which we need to send back.
        const context: TurnContext = this.createContext(activity);
        context.turnState.set('skillMode', true);

        await this.runMiddleware(context, callback);

        // Any Activity responses are now available (via SendActivitiesAsync) so we need to pass back for the response
        return {
            status: 200,
            body: this.getReplies()
        }
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

            responses.push({ id: activity.id });
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
            throw new Error('Missing parameter. reference is required');
        }

        if (!logic) {
            throw new Error('Missing parameter. logic is required');
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

function parseRequest(req: WebRequest): Promise<Activity> {
    return new Promise((resolve: any, reject: any): void => {
        function returnActivity(activity: Activity): void {
            if (typeof activity !== 'object') { throw new Error(`BotFrameworkAdapter.parseRequest(): invalid request body.`); }
            if (typeof activity.type !== 'string') { throw new Error(`BotFrameworkAdapter.parseRequest(): missing activity type.`); }
            if (typeof activity.timestamp === 'string') { activity.timestamp = new Date(activity.timestamp); }
            if (typeof activity.localTimestamp === 'string') { activity.localTimestamp = new Date(activity.localTimestamp); }
            if (typeof activity.expiration === 'string') { activity.expiration = new Date(activity.expiration); }
            resolve(activity);
        }

        if (req.body) {
            try {
                returnActivity(req.body);
            } catch (err) {
                reject(err);
            }
        } else {
            let requestData: string = '';
            req.on('data', (chunk: string) => {
                requestData += chunk;
            });
            req.on('end', () => {
                try {
                    req.body = JSON.parse(requestData);
                    returnActivity(req.body);
                } catch (err) {
                    reject(err);
                }
            });
        }
    });
}