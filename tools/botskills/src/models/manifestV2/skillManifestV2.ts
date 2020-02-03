/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface ISkillManifestV2 {
    $schema: string;
    $id: string;
    name: string;
    description: string;
    publisherName: string;
    version: string;
    iconUrl: string;
    copyright: string;
    license: string;
    privacyUrl: string;
    tags: string[];
    endpoints: IEndpoint[];
    dispatchModels: IDispatchModel;
    activities: Map<string, IActivity>;
    activitiesSent: IActivitySent;
    definitions: IDefinitions;
}

export interface IDefinitions {
    eventInfo: IEventInfo;
    changeEventStatusInfo: IChangeEventStatusInfo;
    timezone: ITimeZone;
    eventSummary: IEventSummary;
}

export interface IEventSummary {
    type: string;
    item: IRef;
}

export interface ITimeZone {
    type: string;
    properties: Map<string, IProperty>;
}

export interface IChangeEventStatusInfo {
    type: string;
    required: string[];
    properties: Map<string, IProperty>;
}

export interface IEventInfo {
    type: string;
    required: string[];
    properties: Map<string, IProperty>;
}

export interface IProperty {
    type: string;
    description: string;
}

export interface IActivitySent {
    type: string;
    description: string;
    additionalProperties: IAnyOf;
}

export interface IAnyOf {
    anyOf: IRef[];
}

export interface IActivity {
    description: string;
    type: string;
    name: string;
    value: IRef;
    resultValue: IRef;
}

export interface IRef {
    ref: string;
}

export interface IEndpoint {
    name: string;
    protocol: string;
    description: string;
    endpointUrl: string;
    msAppId: string;
}

export interface IDispatchModel {
    languages: Map<string, IModel[]>;
    intents: Map<string, string>;
}

export interface IModel {
    id: string;
    name: string;
    contentType: string;
    url: string;
    description: string;
}