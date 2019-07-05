/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface ICognitiveModelFile {
    cognitiveModels: {
        [key: string]: {
            dispatchModel: {
                name: string;
            };
        };
    };
}
