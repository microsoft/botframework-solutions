/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const { join } = require("path");

 // Contains bot responses.
class TestResponses {
    constructor() {
        // Generated accessors
        this.name = TestResponses.name;
    }
}

TestResponses.pathToResource = join(__dirname, 'resources');
TestResponses.getResponseText  = 'GetResponseText';
TestResponses.multiLanguage  = 'MultiLanguage';
TestResponses.englishOnly  = 'EnglishOnly';
TestResponses.noInputHint  = 'NoInputHint';

exports.TestResponses = TestResponses;