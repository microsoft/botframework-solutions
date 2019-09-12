/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface ICognitiveModel {
    cognitiveModels: {
        [key: string]: {
            dispatchModel: {
                name: string;
            };
        };
    };
}
