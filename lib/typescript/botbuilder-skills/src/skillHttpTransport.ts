/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { DefaultHttpClient, HttpClient, HttpOperationResponse, RequestPrepareOptions, WebResource } from '@azure/ms-rest-js';
import { TurnContext } from 'botbuilder';
import { ActivityExtensions, TokenEvents } from 'botbuilder-solutions';
import { Activity, ActivityTypes } from 'botframework-schema';
import { MicrosoftAppCredentialsEx } from './auth';
import { ISkillManifest, SkillEvents } from './models';
import { ISkillTransport, TokenRequestHandler } from './skillTransport';

export class SkillHttpTransport implements ISkillTransport {
    private readonly httpClient: HttpClient;
    private readonly skillManifest: ISkillManifest;
    private readonly appCredentials: MicrosoftAppCredentialsEx;

    /**
     * Http SkillTransport implementation
     */
    constructor(skillManifest: ISkillManifest, appCredentials: MicrosoftAppCredentialsEx, httpClient?: HttpClient) {
        if (!skillManifest) {
            throw new Error('skillManifest has no value');
        }
        this.skillManifest = skillManifest;

        if (!appCredentials) {
            throw new Error('appCredentials has no value');
        }
        this.appCredentials = appCredentials;

        this.httpClient = httpClient || new DefaultHttpClient();
    }

    public async forwardToSkill(turnContext: TurnContext,
                                activity: Partial<Activity>,
                                tokenRequestHandler?: TokenRequestHandler): Promise<boolean> {
        // Serialize the activity and POST to the Skill endpoint
        const requestOptions: RequestPrepareOptions = {
            method: 'POST',
            url: this.skillManifest.endpoint,
            body: activity
        };
        const request: WebResource = new WebResource().prepare(requestOptions);

        MicrosoftAppCredentialsEx.trustServiceUrl(this.skillManifest.endpoint);
        await this.appCredentials.signRequest(request);

        const response: HttpOperationResponse = await this.httpClient.sendRequest(request);

        if (response.status < 200 || response.status >= 300) {
            const result: string = `HTTP error when forwarding activity to the skill: Status Code:${
                response.status
            }, Message: '${
                response.bodyAsText
            }'`;

            await turnContext.sendActivity({
                type: ActivityTypes.Trace,
                text: result
            });

            throw new Error(result);
        }

        const responseBody: Activity[] = response.parsedBody;

        // Retrieve Activity responses
        const skillResponses: Activity[] = responseBody.map(fixActivityTimestamp);

        const filteredResponses: Activity[] = [];

        let endOfConversation: boolean = false;

        skillResponses.forEach(async (skillResponse: Activity) => {
            // Once a Skill has finished it signals that it's handing back control to the parent through a
            // EndOfConversation event which then causes the SkillDialog to be closed. Otherwise it remains "in control".
            if (skillResponse.type === ActivityTypes.EndOfConversation) {
                endOfConversation = true;
            } else if (skillResponse.name === TokenEvents.tokenRequestEventName) {
                if (tokenRequestHandler) {
                    const tokenResponseActivity: Activity|undefined = await tokenRequestHandler(skillResponse);
                    if (tokenResponseActivity) {
                        return this.forwardToSkill(turnContext, tokenResponseActivity);
                    }
                }
            } else {
                // Trace messages are not filtered out and are sent along with messages/events.
                filteredResponses.push(skillResponse);
            }
        });

        // Send the filtered activities back (for example, token requests, EndOfConversation, etc. are removed)
        if (filteredResponses.length > 0) {
            await turnContext.sendActivities(filteredResponses);
        }

        return endOfConversation;
    }

    public async cancelRemoteDialogs(turnContext: TurnContext): Promise<void> {
        const cancelRemoteDialogEvent: Activity = ActivityExtensions.createReply(turnContext.activity);
        cancelRemoteDialogEvent.type = ActivityTypes.Event;
        cancelRemoteDialogEvent.name = SkillEvents.cancelAllSkillDialogsEventName;

        await this.forwardToSkill(turnContext, cancelRemoteDialogEvent);
    }

    public disconnect(): void {
        // doesn't have to do any disconnect for http
    }
}

/**
 * Parse the Date-related properties
 * @param activity Activity to convert
 * @returns Activity with correct Date properties
 */
function fixActivityTimestamp(activity: Activity): Activity {
    if (typeof activity.timestamp === 'string') {
        activity.timestamp = new Date(activity.timestamp);
    }

    if (typeof activity.localTimestamp === 'string') {
        activity.localTimestamp = new Date(activity.localTimestamp);
    }

    if (typeof activity.expiration === 'string') {
        activity.expiration = new Date(activity.expiration);
    }

    return activity;
}
