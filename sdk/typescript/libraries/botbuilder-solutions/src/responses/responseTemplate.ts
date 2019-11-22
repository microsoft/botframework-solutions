/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { InputHints } from 'botframework-schema';
import { IReply } from './reply';

export class ResponseTemplate {
    public replies: IReply[] = [];
    public suggestedActions: string[] = [];
    public inputHint: string = InputHints.AcceptingInput;

    public get reply(): IReply | undefined {
        if (this.replies.length > 0) {
            return this.replies[this.getRandom(this.replies.length)];
        }

        return undefined;
    }

    private getRandom(upper: number): number {
        return Math.floor(Math.random() * upper);
    }
}
