/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConnectedService, ServiceTypes } from 'botframework-config';

export class SkillDefinition extends ConnectedService {

    public dispatchIntent: string = '';

    public assembly: string = '';

    public luisServiceIds: string[] = [];

    public supportedProviders: string[] = [];

    public parameters: string[] = [];

    public configuration: Map<string, string> = new Map();

    constructor() {
        super(undefined, ServiceTypes.Generic);
    }
}
