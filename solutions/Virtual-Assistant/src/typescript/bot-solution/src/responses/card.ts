// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { ICardData } from './cardDataBase';

export class Card {

    constructor (name: string, data : ICardData) {
        this.name = name;
        this.data = data;
    }

    public name: string = '';

    public data : ICardData = '';
}
