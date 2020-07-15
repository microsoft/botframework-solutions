/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext, SkillHandler, ChannelServiceRoutes } from 'botbuilder';
import { ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
import * as restify from 'restify';
import { DefaultAdapter } from './adapters/defaultAdapter';
import container from './inversify.config';
import { TYPES } from './types/constants';
import { MainDialog } from './dialogs/mainDialog';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';

const adapter: DefaultAdapter = container.get<DefaultAdapter>(TYPES.DefaultAdapter);
const handler: SkillHandler = container.get<SkillHandler>(TYPES.SkillHandler);

// Create server
const server: restify.Server = restify.createServer({ maxParamLength: 1000 });

// Enable the Application Insights middleware, which helps correlate all activity
// based on the incoming request.
server.use(restify.plugins.bodyParser());
server.use(restify.plugins.queryParser());
server.use(restify.plugins.authorizationParser());
server.use(ApplicationInsightsWebserverMiddleware);

server.listen(process.env.port || process.env.PORT || '3979', (): void => {
    console.log(` ${ server.name } listening to ${ server.url } `);
    console.log(`Get the Emulator: https://aka.ms/botframework-emulator`);
    console.log(`To talk to your bot, open your '.bot' file in the Emulator`);
});

// Listen for incoming requests
server.post('/api/messages', async (req: restify.Request, res: restify.Response): Promise<void> => {
    const bot: DefaultActivityHandler<MainDialog> = container.get<DefaultActivityHandler<MainDialog>>(TYPES.DefaultActivityHandler);
    // Route received a request to adapter for processing
    await adapter.processActivity(req, res, async (turnContext: TurnContext): Promise<void> => {
        // route to bot activity handler.
        await bot.run(turnContext);
    });
});

// Register the request handler.
const skillEndpoint = new ChannelServiceRoutes(handler);
skillEndpoint.register(server, '/api/skills');