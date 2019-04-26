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

    public getObj(key: string): Object {
        return this.contextStorage[key];
    }

    public setObj(key: string, value: Object): void {
        this.contextStorage[key] = value;
    }

    public tryGet(key: string): { result: boolean; value?: Object } {
        const value: Object|undefined = this.contextStorage[key];

        return { result: !!value, value: value };
    }
}
