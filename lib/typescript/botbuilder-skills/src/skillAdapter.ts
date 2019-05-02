/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotFrameworkAdapter, InvokeResponse, TurnContext, WebRequest, WebResponse } from 'botbuilder';
import { ActivityExtensions, IRemoteUserTokenProvider } from 'botbuilder-solutions';
import { ICredentialProvider, SimpleCredentialProvider } from 'botframework-connector';
import { Activity, ActivityTypes, ConversationReference, ResourceResponse } from 'botframework-schema';
import { v4 as uuid } from 'uuid';

export interface ISkillAdapter {
    processActivitySkill(authHeader: string, activity: Partial<Activity>,
                         callback: (turnContext: TurnContext) => Promise<void>): Promise<InvokeResponse>;
}

/**
 * Skill adapter provides the capability to invoke Skills (Bots) over a direct request.
 * This requires the remote Skill to be leveraging this new adapter on a different endpoint to the usual
 * BotFrameworkAdapter that operates on the /api/messages route.
 */
export class SkillAdapter extends BotFrameworkAdapter implements ISkillAdapter, IRemoteUserTokenProvider {
    private readonly authHeaderName: string = 'Authorization';

    private readonly credentialProvider: ICredentialProvider;
    private readonly queuedActivities: Partial<Activity>[];

    public constructor(credentialProvider?: ICredentialProvider) {
        super();
        this.credentialProvider = credentialProvider || new SimpleCredentialProvider('', '');
        this.queuedActivities = [];
    }

    public async processActivity(req: WebRequest, res: WebResponse,
                                 logic: (context: TurnContext) => Promise<void>): Promise<void> {
        // deserialize the incoming Activity
        const activity: Activity = await parseRequest(req);

        // grab the auth header from the inbound http request
        const headers: { [header: string]: string | string[] | undefined } = req.headers;
        const authHeader: string = <string> headers[this.authHeaderName];

        // process the inbound activity with the bot
        const invokeResponse: InvokeResponse = await this.processActivitySkill(authHeader, activity, logic);

        // write the response, potentially serializing the InvokeResponse
        res.status(invokeResponse.status);
        if (invokeResponse.body) {
            res.send(invokeResponse.body);
        }

        res.end();
    }

    public async continueConversation(
        reference: Partial<ConversationReference>,
        logic: (revocableContext: TurnContext) => Promise<void>): Promise<void> {

        if (!reference) {
            throw new Error('Missing parameter. reference is required');
        }

        if (!logic) {
            throw new Error('Missing parameter. logic is required');
        }

        const context: TurnContext = new TurnContext(this, ActivityExtensions.getContinuationActivity(reference));
        await this.runMiddleware(context, logic);
    }

    public async sendActivities(context: TurnContext, activities: Partial<Activity>[]): Promise<ResourceResponse[]> {
        const responses: ResourceResponse [] = [];
        const proactiveActivities: Partial<Activity>[] = [];

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
        throw new Error('Method not implemented.');
    }

    public updateActivity(context: TurnContext, activity: Partial<Activity>): Promise<void> {
        throw new Error('Method not implemented.');
    }

    public async sendRemoteTokenRequestEvent(turnContext: TurnContext): Promise<void> {
        // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
        // PENDING C# Error handling - if we get a new activity that isn't an event
        const response: Activity = ActivityExtensions.createReply(turnContext.activity);
        response.type = ActivityTypes.Event;
        response.name = 'tokens/request';

        // Send the tokens/request Event
        await this.sendActivities(turnContext, [ response ]);
    }

    public async processActivitySkill(authHeader: string, activity: Partial<Activity>,
                                      callback: (revocableContext: TurnContext) => Promise<void>): Promise<InvokeResponse> {
        // Ensure the Activity has been retrieved from the HTTP POST
        // Not performing authentication checks at this time

        // Process the Activity through the Middleware and the Bot, this will generate Activities which we need to send back.
        const context: TurnContext = this.createContext(activity);

        await this.runMiddleware(context, callback);

        // Any Activity responses are now available (via SendActivitiesAsync) so we need to pass back for the response
        return {
            status: 200,
            body: this.getReplies()
        };
    }

    public getReplies(): Partial<Activity>[] {
        return this.queuedActivities.reverse();
    }
}

function sleep(delay: number): Promise<void> {
    return new Promise<void>((resolve: (value: void) => void): void => {
        setTimeout(resolve, delay);
    });
}

function parseRequest(req: WebRequest): Promise<Activity> {
    // tslint:disable-next-line:typedef
    return new Promise((resolve, reject): void => {
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
