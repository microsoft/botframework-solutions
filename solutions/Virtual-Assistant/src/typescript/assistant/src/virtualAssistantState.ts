// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export class VirtualAssistantState {
    public lastIntent: string;

    constructor(lastIntent: string) {
        this.lastIntent = lastIntent;
    }
}
