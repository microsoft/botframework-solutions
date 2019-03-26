/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConnectedService, ServiceTypes } from 'botframework-config';

export class SkillEvent extends ConnectedService {

    public event: string = '';

    public skillIds: string[] = [];

    public parameters: Map<string, string> = new Map();

    constructor() {
        super(undefined, ServiceTypes.Generic);
    }
}
