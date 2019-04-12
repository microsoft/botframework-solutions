// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ResponseManager, SkillConfigurationBase } from 'botbuilder-solutions';
import { BotTelemetryClient, ConversationState, UserState } from 'botbuilder';
import { TurnContext } from 'botbuilder-core';
import { DialogSet } from 'botbuilder-dialogs';
import { MainDialog } from './dialogs/main/mainDialog';
import { MainResponses } from './dialogs/main/mainResponses';
import { SampleResponses } from './dialogs/sample/sampleResponses';
import { SharedResponses } from './dialogs/shared/sharedResponses';
import { IServiceManager } from './serviceClients/IServiceManager';
import { ServiceManager } from './serviceClients/serviceManager';

import { SampleDialog } from './dialogs/sample/sampleDialog';
/**
 * Here is the documentation of the <%=skillTemplateName%> class
 */
export class <%=skillTemplateName%> {

    private readonly services : SkillConfigurationBase;
    private readonly responseManager : ResponseManager;
    private readonly conversationState: ConversationState;
    private readonly userState: UserState;
    private readonly telemetryClient: BotTelemetryClient;
    private readonly serviceManager: IServiceManager;
    private dialogs: DialogSet;
    private skillMode: boolean = false;

    constructor (
        services: SkillConfigurationBase,
        conversationState: ConversationState,
        userState: UserState,
        telemetryClient: BotTelemetryClient,
        skillMode: boolean = false,
        responseManager: ResponseManager | undefined,
        serviceManager: IServiceManager | undefined) {

        this.skillMode = skillMode;
        if (services === undefined) {
            throw new Error('services parameter is null');
        }
        this.services = services;
        if (userState === undefined) {
            throw new Error ('userState parameter is null');
        }
        this.userState = userState;
        if (conversationState === undefined) {
            throw new Error ('conversationState parameter is null');
        }
        this.conversationState = conversationState;
        if (telemetryClient === undefined) {
            throw new Error ('telemetryClient parameter is null');
        }
        this.telemetryClient = telemetryClient;
        if (serviceManager === undefined) {
            this.serviceManager = new ServiceManager();
        } else {
            this.serviceManager = serviceManager;
        }
        if (responseManager === undefined) {
            this.responseManager = new ResponseManager(
                Array.from(this.services.localeConfigurations.keys()),
                [SampleResponses, MainResponses, SharedResponses]
            );
        } else {
            this.responseManager = responseManager;
        }

        this.dialogs = new DialogSet(this.conversationState.createProperty('DialogState'));
        this.dialogs.add(new MainDialog (
            this.services,
            this.responseManager,
            this.conversationState,
            this.userState,
            this.telemetryClient,
            this.serviceManager,
            this.skillMode
             ));
        }
        /**
         * Run every turn of the conversation. Handles orchestration of messages.
         */
        public async onTurn (turnContext: TurnContext): Promise<void> {
            // tslint:disable-next-line:no-any
            const dc: any = await this.dialogs.createContext(turnContext);

            if (dc.activeDialog !== undefined) {
                // tslint:disable-next-line:no-any
                const result: any = await dc.continueDialog();
            } else {
                await dc.beginDialog(MainDialog.name);
            }
        }
}
