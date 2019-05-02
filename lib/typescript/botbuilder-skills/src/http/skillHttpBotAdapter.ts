import { BotAdapter, BotTelemetryClient, InvokeResponse, Severity, TurnContext } from 'botbuilder';
import { ActivityExtensions, IRemoteUserTokenProvider, TelemetryExtensions } from 'botbuilder-solutions';
import { Activity, ActivityTypes, ConversationReference, ResourceResponse } from 'botframework-schema';
import { v4 as uuid } from 'uuid';
import { BotCallbackHandler, IActivityHandler } from '../activityHandler';

export class SkillHttpBotAdapter extends BotAdapter implements IActivityHandler, IRemoteUserTokenProvider {
    private readonly telemetryClient: BotTelemetryClient;
    private readonly queuedActivities: Partial<Activity>[];

    constructor(telemetryClient: BotTelemetryClient) {
        super();
        this.telemetryClient = telemetryClient;
        this.queuedActivities = [];
    }

    public async sendActivities(context: TurnContext, activities: Partial<Activity>[]): Promise<ResourceResponse[]> {
        const responses: ResourceResponse[] = [];

        activities.forEach(async (activity: Partial<Activity>) => {
            if (!activity.id) {
                activity.id = uuid();
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
                await sleep(delayMs);
            } else if (activity.type === ActivityTypes.Trace && activity.channelId !== 'emulator') {
                // if it is a Trace activity we only send to the channel if it's the emulator.
            } else if (activity.type === ActivityTypes.Typing && activity.channelId !== 'test') {
                // If it's a typing activity we omit this in test scenarios to avoid test failures
            } else {
                // Queue up this activity for aggregation back to the calling Bot in one overall message.
                this.queuedActivities.push(activity);
            }

            responses.push({ id: activity.id });
        });

        return responses;
    }

    public deleteActivity(context: TurnContext, reference: Partial<ConversationReference>): Promise<void> {
        throw new Error('Http Request/Response model doesn\'t support DeleteActivityAsync call!');
    }

    public updateActivity(context: TurnContext, activity: Partial<Activity>): Promise<void> {
        throw new Error('Http Request/Response model doesn\'t support UpdateActivityAsync call!');
    }

    public continueConversation(reference: Partial<ConversationReference>, logic: BotCallbackHandler): Promise<void> {
        throw new Error('Http Request/Response model doesn\'t support ContinueConversationAsync call!');
    }

    public async processActivity(activity: Activity, callback: BotCallbackHandler): Promise<InvokeResponse> {
        const messageIn: string = `SkillHttpBotAdapter: Received an incoming activity. Activity id: ${activity.id}`;
        TelemetryExtensions.trackTraceEx(this.telemetryClient, messageIn, Severity.Information, activity);

        // Process the Activity through the Middleware and the Bot, this will generate Activities which we need to send back.
        const context: TurnContext = new TurnContext(this, activity);

        await this.runMiddleware(context, callback);

        const messageOut: string = `SkillHttpBotAdapter: Batching activities in the response. ReplyToId: ${activity.id}`;
        TelemetryExtensions.trackTraceEx(this.telemetryClient, messageOut, Severity.Information, activity);

        // Any Activity responses are now available (via SendActivitiesAsync) so we need to pass back for the response
        return {
            status: 200,
            body: this.queuedActivities
        };
    }

    public async sendRemoteTokenRequestEvent(turnContext: TurnContext): Promise<void> {
        // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
        const response: Activity = ActivityExtensions.createReply(turnContext.activity);
        response.type = ActivityTypes.Event;
        response.name = 'tokens/request';

        // Send the tokens/request Event
        await this.sendActivities(turnContext, [response]);
    }
}

function sleep(delay: number): Promise<void> {
    return new Promise<void>((resolve: (value: void) => void): void => {
        setTimeout(resolve, delay);
    });
}
