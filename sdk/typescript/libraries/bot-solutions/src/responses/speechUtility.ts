/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Choice, PromptOptions } from 'botbuilder-dialogs';
import { Activity, Attachment } from 'botframework-schema';
import { ListEx } from '../extensions';
import { CommonResponses } from '../resources';
import { readFileSync } from 'fs';
import { ResponsesUtil } from '../util/responsesUtil';

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

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace SpeechUtility {

    export function listToSpeechReadyString(
        toProcess: PromptOptions|Activity,
        locale: string,
        readOrder: ReadPreference = ReadPreference.Enumeration,
        maxSize: number = 4
    ): string {

        let speakStrings: string[] = [];
        let parent = '';

        if ((toProcess as PromptOptions).choices !== undefined) {
            const selectOption: PromptOptions = toProcess as PromptOptions;
            const choices: (string|Choice)[] = selectOption.choices || [];
            speakStrings = choices.map((value: string|Choice): string => typeof(value) === 'string' ? value : value.value);
            parent = typeof(selectOption.prompt) === 'string'
                ? (selectOption.prompt || '')
                : (selectOption.prompt ? (selectOption.prompt.text || '') : '');
        } else {
            const activity: Activity = toProcess as Activity;
            const attachments: Attachment[] = activity.attachments || [];
            speakStrings = attachments.map((value: Attachment): string => value.content.speak as string);
            parent = activity.speak || '';
        }

        return listToSpeech(parent, speakStrings, readOrder, maxSize, locale);
    }

    function listToSpeech(parent: string, selectionStrings: string[], readOrder: ReadPreference, maxSize: number, locale: string): string {

        const result: string = `${ parent } ` || '';

        const itemDetails: string[] = [];
        const readSize: number = Math.min(selectionStrings.length, maxSize);
        const jsonPath: string = ResponsesUtil.getResourcePath(CommonResponses.name, CommonResponses.pathToResource, locale);
        const commonFile: string = readFileSync(jsonPath, 'utf8');

        if (readSize === 1) {
            itemDetails.push(selectionStrings[0]);
        } else {
            for (let index = 0; index < readSize; index = index + 1) {
                let readFormat = '';
                if (index === 0) {
                    if (readOrder === ReadPreference.Chronological) {
                        readFormat = JSON.parse(commonFile)['latestItem'];
                    } else {
                        readFormat = JSON.parse(commonFile)['firstItem'];
                    }
                } else {
                    if (index === readSize - 1) {
                        readFormat = JSON.parse(commonFile)['lastItem'];
                    } else {
                        if (index === 1) {
                            readFormat = JSON.parse(commonFile)['secondItem'];
                        } else if (index === 2) {
                            readFormat = JSON.parse(commonFile)['thirdItem'];
                        } else if (index === 3) {
                            readFormat = JSON.parse(commonFile)['fourthItem'];
                        }
                    }
                }

                const selectionDetail: string = readFormat.replace('{0}', selectionStrings[index]);
                itemDetails.push(selectionDetail);
            }
        }


        return result + ListEx.toSpeechString(itemDetails, JSON.parse(commonFile)['and'], locale);
    }
}
