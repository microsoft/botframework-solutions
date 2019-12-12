
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { RecognizerResult } from 'botbuilder';

export class SkillState {
    public readonly token: string = '';
    public luisResult: RecognizerResult | undefined;
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    public clear(): void {}
}
