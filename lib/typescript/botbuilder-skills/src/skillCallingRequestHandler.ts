/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import { Activity, ActivityTypes, ResourceResponse } from 'botframework-schema';
import { ReceiveRequest, Response, RequestHandler } from 'microsoft-bot-protocol';
import { IRouteTemplate, Router, IRouteContext } from './protocol';
import { TokenEvents } from 'botbuilder-solutions';

export declare type ActivityAction = (activity: Activity) => Promise<void>;

export class SkillCallingRequestHandler extends RequestHandler {
    private readonly router: Router;
    private readonly turnContext: TurnContext;
    private readonly tokenRequestHandler?: ActivityAction;
    private readonly handoffActivityHandler?: ActivityAction;

    constructor(
        turnContext: TurnContext,
        tokenRequestHandler?: ActivityAction,
        handoffActivityHandler?: ActivityAction
    ) {
        super();
        this.turnContext = turnContext;
        this.tokenRequestHandler = tokenRequestHandler;
        this.handoffActivityHandler = handoffActivityHandler;

        const postRoute: IRouteTemplate = {
            method: 'POST',
            path: '/activities/{activityId}',
            action: {
                action: async (request: ReceiveRequest, routeData: Object) => {            
                    // MISSING Check response converter
                    const bodyParts: string[] = await Promise.all(request.Streams.map(s => s.readAsJson()));
                    const body = bodyParts.join();
                    const activity: Activity = JSON.parse(body);
                    if (!activity) {
                        throw new Error('Error deserializing activity response!');
                    }

                    if (activity.type === ActivityTypes.Event && activity.name === TokenEvents.tokenRequestEventName) {
                        if (this.tokenRequestHandler) {
                            this.tokenRequestHandler(activity);

                            return <ResourceResponse>{};
                        } else {
                            throw new Error('Skill is requesting for token but there\'s no handler on the calling side!');
                        }
                    } else if (activity.type === ActivityTypes.EndOfConversation) {
                        if (this.handoffActivityHandler) {
                            this.handoffActivityHandler(activity);
                            
                            return <ResourceResponse>{};
                        } else {
                            throw new Error('Skill is sending handoff activity but there\'s no handler on the calling side!');
                        }
                    } else {
                        return this.turnContext.sendActivity(activity);
                    }
                }
            }
        };
        
        const putRoute: IRouteTemplate = {
            method: 'PUT',
            path: '/activities/{activityId}',
            action: {
                action: async (request: ReceiveRequest, routeData: Object) => {            
                    // MISSING Check response converter
                    const bodyParts: string[] = await Promise.all(request.Streams.map(s => s.readAsJson()));
                    const body = bodyParts.join();
                    const activity: Activity = JSON.parse(body);
                    await this.turnContext.updateActivity(activity);
                    
                    return undefined;
                }
            }
        };
        
        const deleteRoute: IRouteTemplate = {
            method: 'DELETE',
            path: '/activities/{activityId}',
            action: {
                action: async (request: ReceiveRequest, routeData: Object) => {            
                    // MISSING Check response converter
                    const activityIdProp: [string, string] = Object.entries(routeData).find((e) => e[0] === 'activityId') || ['activityId', ''];
                    const activityId = activityIdProp[1];
                    await this.turnContext.deleteActivity(activityId);
                    
                    return undefined;
                }
            }
        };
        const routes: IRouteTemplate[] = [ postRoute, putRoute, deleteRoute ];
        this.router = new Router(routes);
    }

    public async processRequestAsync(request: ReceiveRequest, logger?: any): Promise<Response> {
        const routeContext: IRouteContext|undefined = this.router.route(request);
        if (routeContext) {
            try {
                const responseBody: Object|undefined = await routeContext.action.action(request, routeContext.routerData);
                // MISSING Response.OK(new StringContent(JsonConvert.SerializeObject(responseBody, SerializationSettings.DefaultSerializationSettings), Encoding.UTF8, SerializationSettings.ApplicationJson));
                const response: Response = Response.create(200);
                response.setBody(responseBody);

                return response;
            } catch (error) {
                return Response.create(500);
            }
        } else {
            return Response.create(404);
        }
    }
}
