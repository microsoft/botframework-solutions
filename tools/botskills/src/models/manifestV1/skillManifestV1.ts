/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface ISkillManifestV1 {
    id: string;
    name: string;
    msaAppId: string;
    endpoint: string;
    description: string;
    suggestedAction: string;
    iconUrl: string;
    authenticationConnections: IAuthenticationConnection[];
    actions: IAction[];
}

export interface IUtteranceSource {
    locale: string;
    source: string[];
}

export interface ISlot {
    name: string;
    types: string[];
}

export interface IUtterance {
    locale: string;
    text: string[];
}

export interface ITriggers {
    utterances: IUtterance[];
    utteranceSources: IUtteranceSource[];
    events: IEvent[];
}

export interface IEvent {
    name: string;
}

export interface IAuthenticationConnection {
    id: string;
    serviceProviderId: string;
    scopes: string;
}

export interface IActionDefinition {
    description: string;
    slots: ISlot[];
    triggers: ITriggers;
}

export interface IAction {
    id: string;
    definition: IActionDefinition;
}
