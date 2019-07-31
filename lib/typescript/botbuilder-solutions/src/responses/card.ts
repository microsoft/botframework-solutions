/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ICardData } from './cardData';

export class Card {

    public constructor(name?: string, data?: ICardData) {
        this.name = name || '';
        this.data = data || {};
    }

    public name: string;
    public data: ICardData;
}
