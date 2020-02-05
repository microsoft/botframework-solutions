/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ModelResult } from '@microsoft/recognizers-text';
import { recognizeBoolean } from '@microsoft/recognizers-text-choice';
import { PromptRecognizerResult } from 'botbuilder-dialogs';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace ConfirmRecognizerHelper {
    const valueKey = 'value';

    export function confirmYerOrNo(utterance: string, locale: string = ''): PromptRecognizerResult<boolean> {
        let result: PromptRecognizerResult<boolean> = { succeeded: false };

        if (utterance !== undefined && utterance.trim().length > 0) {
            // Recognize utterance
            const results: ModelResult[] = recognizeBoolean(utterance, locale);

            if (results.length > 0) {
                const first: ModelResult = results[0];
                const value: boolean = first.resolution[valueKey] as boolean;
                if (value !== undefined) {
                    result = {
                        succeeded: true,
                        value: value
                    };
                }
            }
        }

        return result;
    }
}
