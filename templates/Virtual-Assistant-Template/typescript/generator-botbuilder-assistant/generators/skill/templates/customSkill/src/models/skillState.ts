
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { RecognizerResult } from 'botbuilder';

export class SkillState {
    public readonly token: string = '';
    public luisResult: RecognizerResult | undefined;
    // tslint:disable-next-line: no-empty
    public clear(): void {}
}
