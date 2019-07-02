/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient, InvokeResponse } from 'botbuilder';
import { Activity } from 'botframework-schema';
import { ContentStream, ReceiveRequest, RequestHandler, Response } from 'microsoft-bot-protocol';
import { BotCallbackHandler, IActivityHandler } from '../activityHandler';

export class SkillWebSocketRequestHandler extends RequestHandler {
    private readonly telemetryClient: BotTelemetryClient;
    public bot!: BotCallbackHandler;
    public activityHandler!: IActivityHandler;

    public constructor(telemetryClient: BotTelemetryClient) {
        super();
        this.telemetryClient = telemetryClient;
    }

    // eslint-disable-next-line @typescript-eslint/tslint/config, @typescript-eslint/no-explicit-any
    public async processRequestAsync(request: ReceiveRequest, logger?: any): Promise<Response> {
        if (this.bot === undefined) { throw new Error(('Missing parameter.  "bot" is required')); }
        if (this.activityHandler === undefined) { throw new Error(('Missing parameter.  "activityHandler" is required')); }

        const response: Response = new Response();
        // MISSING: await request.readBodyAsString();
        const bodyParts: string[] = await Promise.all(request.Streams.map((s: ContentStream): Promise<string> => s.readAsString()));
        const body: string = bodyParts.join();

        if (!body || request.Streams.length === 0) {
            response.statusCode = 400;

            return response;
        }

        if (request.Streams.some((x: ContentStream): boolean => x.payloadType !== 'application/json; charset=utf-8')) {
            response.statusCode = 406;

            return response;
        }

        try {
            // eslint-disable-next-line @typescript-eslint/tslint/config
            const activity: Activity = JSON.parse(body);
            const begin: [number, number] = process.hrtime();
            const invokeResponse: InvokeResponse = await this.activityHandler.processActivity(activity, this.bot);
            const end: [number, number] = process.hrtime(begin);

            const latency: { latency: number } = { latency: toMilliseconds(end) };

            const event: string = 'SkillWebSocketProcessRequestLatency';
            this.telemetryClient.trackEvent({
                name: event,
                metrics: latency
            });

            if (invokeResponse === undefined) {
                response.statusCode = 200;
            } else {
                response.statusCode = invokeResponse.status;
                if (invokeResponse.body) {
                    response.setBody(invokeResponse.body);
                }
            }
        } catch (error) {
            // eslint-disable-next-line @typescript-eslint/tslint/config
            this.telemetryClient.trackException({ exception: error });
            response.statusCode = 500;
        }

        return response;
    }
}

function toMilliseconds(hrtime: [number, number]): number {
    const nanoseconds: number = (hrtime[0] * 1e9) + hrtime[1];

    return nanoseconds / 1e6;
}
