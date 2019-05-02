/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { InvokeResponse } from 'botbuilder';
import { Activity } from 'botframework-schema';
import { ContentStream, ReceiveRequest, RequestHandler, Response } from 'microsoft-bot-protocol';
import { BotCallbackHandler, IActivityHandler } from '../activityHandler';

export class SkillWebSocketRequestHandler extends RequestHandler {
    public bot: BotCallbackHandler;
    public activityHandler: IActivityHandler;

    constructor(activityHandler: IActivityHandler, bot: BotCallbackHandler) {
        super();
        this.activityHandler = activityHandler;
        this.bot = bot;
    }

    // tslint:disable-next-line:no-any
    public async processRequestAsync(request: ReceiveRequest, logger?: any): Promise<Response> {
        if (!this.bot) { throw new Error(('Missing parameter.  "instance" is required')); }
        if (!this.activityHandler) { throw new Error(('Missing parameter.  "instance" is required')); }

        const response: Response = new Response();
        // MISSING: await request.readBodyAsString();
        const bodyParts: string[] = await Promise.all(request.Streams.map((s: ContentStream) => s.readAsString()));
        const body: string = bodyParts.join();

        if (!body || request.Streams.length === 0) {
            response.statusCode = 400;

            return response;
        }

        if (request.Streams.some((x: ContentStream) => x.payloadType !== 'application/json; charset=utf-8')) {
            response.statusCode = 406;

            return response;
        }

        try {
            const activity: Activity = JSON.parse(body);
            const invokeResponse: InvokeResponse = await this.activityHandler.processActivity(activity, this.bot);

            if (!invokeResponse) {
                response.statusCode = 200;
            } else {
                response.statusCode = invokeResponse.status;
                if (invokeResponse.body) {
                    response.setBody(invokeResponse.body);
                }
            }
        } catch (error) {
            response.statusCode = 500;
        }

        return response;
    }
}
