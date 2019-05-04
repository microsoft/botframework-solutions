/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * Context to share state between Bots and Skills.
 */
export class SkillContext {
    private readonly contextStorage: { [key: string]: Object };

    constructor(contextStorage?: { [key: string]: Object }) {
        this.contextStorage = contextStorage || {};
    }

    public get count(): number {
        return Object.keys(this.contextStorage).length;
    }

    public getObj(key: string): Object|undefined {
        return this.contextStorage[key];
    }

    public setObj(key: string, value: Object): void {
        this.contextStorage[key] = value;
    }
}
