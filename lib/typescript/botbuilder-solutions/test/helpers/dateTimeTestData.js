/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

class DateTimeTestData {
    constructor(culture, inputDateTime, expectedDateSpeech, expectedDateSpeechWithSuffix, expectedTimeSpeech, expectedTimeSpeechWithSuffix) {
        this.culture = culture;
        this.inputDateTime = inputDateTime;
        this.expectedDateSpeech = expectedDateSpeech;
        this.expectedDateSpeechWithSuffix = expectedDateSpeechWithSuffix;
        this.expectedTimeSpeech = expectedTimeSpeech;
        this.expectedTimeSpeechWithSuffix = expectedTimeSpeechWithSuffix;
    }
}

exports.DateTimeTestData = DateTimeTestData