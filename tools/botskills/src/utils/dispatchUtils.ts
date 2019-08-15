import { ICognitiveModelFile } from '../models';

/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * @param cognitiveModelsFile cognitiveModels assistant file which contains the dispatchs.
 * @returns Returns a map containing the culture with the related dispatchName
 */
// tslint:disable-next-line:export-name
export function getDispatchNames(cognitiveModelsFile: ICognitiveModelFile): Map<string, string> {
    try {
        const dispatchNames: Map<string, string> = new Map();
        for (const [key, value] of Object.entries(cognitiveModelsFile.cognitiveModels)) {
            if (value.dispatchModel !== undefined && value.dispatchModel.name !== undefined) {
                dispatchNames.set(key, value.dispatchModel.name);
            }
        }

        return dispatchNames;
    } catch (err) {
        throw err;
    }
}
