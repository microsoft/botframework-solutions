/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Choice, PromptOptions } from 'botbuilder-dialogs';
import { Activity, Attachment } from 'botframework-schema';
import i18next from 'i18next';
import { ListExtensions } from '../extensions/listExtensions';

/**
 * Read order of list items.
 */
export enum ReadPreference {
    /**
     * First item, second item, third item, etc.
     */
    Enumeration,
    /**
     * Latest item, second item, third item, etc.
     */
    Chronological
}

export namespace SpeechUtility {
    const breakString: string = '<break/>';

    export function listToSpeechReadyString(toProcess: PromptOptions|Activity,
                                            readOrder: ReadPreference = ReadPreference.Enumeration,
                                            maxSize: number = 4): string {

        let speakStrings: string[] = [];
        let parent: string = '';

        if ((<PromptOptions>toProcess).choices !== undefined) {
            const selectOption: PromptOptions = <PromptOptions>toProcess;
            const choices: (string|Choice)[] = selectOption.choices || [];
            speakStrings = choices.map((value: string|Choice) => typeof(value) === 'string' ? value : value.value);
            parent = typeof(selectOption.prompt) === 'string'
                ? (selectOption.prompt || '')
                : (selectOption.prompt ? (selectOption.prompt.text || '') : '');
        } else {
            const activity: Activity = <Activity>toProcess;
            const attachments: Attachment[] = activity.attachments || [];
            speakStrings = attachments.map((value: Attachment) => value.content.speak);
            parent = activity.speak || '';
        }

        return listToSpeech(parent, speakStrings, readOrder, maxSize);
    }

    function listToSpeech(parent: string, selectionStrings: string[], readOrder: ReadPreference, maxSize: number): string {
        let result: string = '';

        if (parent) {
            result = parent + breakString;
        }

        const itemDetails: string[] = [];
        const readSize: number = Math.min(selectionStrings.length, maxSize);

        if (readSize === 1) {
            itemDetails.push(selectionStrings[0]);
        } else {
            for (const index: number = 0; index < readSize; index + 1) {
                let readFormat: string = '';
                if (index === 0) {
                    if (readOrder === ReadPreference.Chronological) {
                        readFormat = i18next.t('common:latestItem');
                    } else {
                        readFormat = i18next.t('common:firstItem');
                    }
                } else {
                    if (index === readSize - 1) {
                        readFormat = i18next.t('common:lastItem');
                    } else {
                        if (index === 1) {
                            readFormat = i18next.t('common:secondItem');
                        } else if (index === 2) {
                            readFormat = i18next.t('common:thirdItem');
                        } else if (index === 3) {
                            readFormat = i18next.t('common:fourthItem');
                        }
                    }
                }

                const selectionDetail: string = i18next.t(readFormat, selectionStrings[index]);
                itemDetails.push(selectionDetail);
            }
        }

        return result + ListExtensions.toSpeechString(itemDetails, i18next.t('common:and'));
    }
}
