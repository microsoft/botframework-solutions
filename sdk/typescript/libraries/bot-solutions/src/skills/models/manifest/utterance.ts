/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * Utterances for a given locale which form part of an Action within a manifest.
 */
export interface IUtterance {
    locale: string;
    text: string[];
}
