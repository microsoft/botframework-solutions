/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { MicrosoftAppCredentials } = require("botframework-connector");

class MockMicrosoftAppCredentials extends MicrosoftAppCredentials {

    async getToken(forceRefresh = false) {
        return "";
    }

    async processHttpRequest(request){
        return Promise.resolve();
    }
}

exports.MockMicrosoftAppCredentials = MockMicrosoftAppCredentials;