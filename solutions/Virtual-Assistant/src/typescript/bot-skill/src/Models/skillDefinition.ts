/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export class SkillDefinition {

    public name: string = '';
    public dispatchIntent: string = '';
    public endpoint: string = '';
    public supportedProviders: string[] = [];
    public scope: string = '';
}
