/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * Source of utterances for a given locale which form part of an Action within a manifest.
 */
export interface IUtteranceSource {
    locale: string;
    source: string[];
}
