/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import { Activity } from 'botframework-schema';
import { ReceiveRequest, Response, RequestHandler } from 'microsoft-bot-protocol';
import { IRouteTemplate, Router, IRouteContext } from './protocol';

export declare type ActivityAction = () => Activity;

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
        // PENDING
        const routes: IRouteTemplate[] = [];
        this.router = new Router(routes);
    }

    public async processRequestAsync(request: ReceiveRequest, logger?: any): Promise<Response> {
        const routeContext: IRouteContext|undefined = this.router.route(request);
        if (routeContext) {
            try {
                const responseBody: Object = await routeContext.action.action(request, routeContext.routerData);
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