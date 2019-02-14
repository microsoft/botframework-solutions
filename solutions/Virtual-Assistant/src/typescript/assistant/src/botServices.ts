// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

/**
 * Represents references to external services.
 * For example, LUIS services are kept here as a singleton. This external service is configured
 * using the BotConfiguration class.
 */
export class BotServices {
    private attribute: string = '';

    /**
     * Initializes a new instance of the BotServices class.
     */
    constructor(attribute: string) {
        this.attribute = attribute;
    }
}
