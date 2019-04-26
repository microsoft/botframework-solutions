/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * The SkillManifest class models the Skill Manifest which is used to express the capabilities
 * of a skill and used to drive Skill configuration and orchestration.
 */
export interface ISkillManifest {
    id: string;
    msAppId: string;
    name: string;
    endpoint: string;
    description: string;
    iconUrl: string;
    authenticationConnections: IAuthenticationConnection[];
    actions: IAction[];
}

/**
 * Describes an Authentication connection that a Skill requires for operation.
 */
export interface IAuthenticationConnection {
    id: string;
    serviceProviderId: string;
    scopes: string;
}

export interface IAction {
    id: string;
    definition: IActionDefinition;
}

/**
 * Definition of a Manifest Action. Describes how an action is trigger and any slots (parameters) it accepts.
 */
export interface IActionDefinition {
    description: string;
    slots: ISlot[];
    triggers: ITriggers;
}

/**
 * Definition of the triggers for a given action within a Skill.
 */
export interface ITriggers {
    utterances: IUtterance[];
    utteranceSources: IUtteranceSources[];
    events: IEvent[];
}

/**
 * Utterances for a given locale which form part of an Action within a manifest.
 */
export interface IUtterance {
    locale: string;
    text: string[];
}

/**
 * Source of utterances for a given locale which form part of an Action within a manifest.
 */
export interface IUtteranceSources {
    locale: string;
    source: string[];
}

export interface IEvent {
    name: string;
}

/**
 * Slot definition for a given Action within a Skill manifest.
 */
export interface ISlot {
    name: string;
    types: string[];
}
