/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TelemetryClient } from 'applicationinsights';
import {
    AutoSaveStateMiddleware,
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    ConversationState,
    ShowTypingMiddleware,
    StatePropertyAccessor,
    UserState } from 'botbuilder';
import { DialogState } from 'botbuilder-dialogs';
import {
    SkillContext,
    SkillHttpAdapter,
    SkillHttpBotAdapter,
    SkillMiddleware } from 'botbuilder-skills';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware,
    TelemetryLoggerMiddleware} from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings';

export class CustomSkillAdapter extends SkillHttpBotAdapter {

    constructor(
        settings: Partial<IBotSettings>,
        userState: UserState,
        conversationState: ConversationState,
        telemetryClient: TelemetryClient,
        skillContextAccessor: StatePropertyAccessor<SkillContext>,
        dialogStateAccesor: StatePropertyAccessor<DialogState>
    ) {
        super(
            telemetryClient
        );

        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        // Currently not working https://github.com/Microsoft/botbuilder-js/issues/853#issuecomment-481416004
        // this.use(new TranscriptLoggerMiddleware(this.transcriptStore));
        // Typing Middleware (automatically shows typing when the bot is responding/working)
        this.use(new ShowTypingMiddleware());
        let defaultLocale: string = 'en-us';
        if (settings.defaultLocale !== undefined) {
            defaultLocale = settings.defaultLocale;
        }
        this.use(new SetLocaleMiddleware(defaultLocale));
        this.use(new EventDebuggerMiddleware());
        // Use the AutoSaveStateMiddleware middleware to automatically read and write conversation and user state.
        this.use(new AutoSaveStateMiddleware(conversationState, userState));
        this.use(new SkillMiddleware(conversationState, skillContextAccessor, dialogStateAccesor));
    }
}
