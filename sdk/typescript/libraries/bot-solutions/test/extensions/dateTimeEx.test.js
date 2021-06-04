/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

require("dayjs/locale/de");
require("dayjs/locale/es");
require("dayjs/locale/it");
require("dayjs/locale/fr");
require("../../lib/resources/customizeLocale/zh");
const { strictEqual } = require("assert");
const dayjs = require("dayjs");
const { join } = require("path");
const { DateTimeTestData } = require(join("..", "helpers", "dateTimeTestData"));
const { readFileSync } = require("fs");
const { DateTimeEx } = require(join("..", "..", "lib", "extensions", "dateTimeEx"));
const { CommonResponses } = require(join("..", "..", "lib", "resources"));
const { ResponsesUtil } = require(join("..", "..", "lib", "util"));

describe("date time extensions", function() {
    describe("using explicit value", function() {
        describe("validate date time extension outputs from random dates", function(){
            // Setup test data
            const today = new Date();
            const tomorrow = new Date();
            tomorrow.setDate(today.getDate() + 1);
            const specificDate = new Date("1975-04-04T01:20:42");
            const specificDatePluralHour = new Date("1975-04-04T04:30:42");
            const testData = [];
            const englishUsCulture = "en-US";
            const spanishSpainCulture = "es-ES";
            const spanishMexicoCulture = "es-MX";

            // US English
            const dateEnUsToday = new DateTimeTestData(
                englishUsCulture,
                today,
                "Today",
                "Today",
                today.toLocaleTimeString(),
                `at ${today.toLocaleTimeString()}`);
            const dateEnUsTomorrow = new DateTimeTestData(
                englishUsCulture,
                tomorrow,
                "Tomorrow",
                "Tomorrow",
                tomorrow.toLocaleTimeString(),
                `at ${tomorrow.toLocaleTimeString()}`);
            const dateEnUsSpecificDate = new DateTimeTestData(
                englishUsCulture,
                specificDate,
                "Friday, April 04",
                "Friday, April 04",
                specificDate.toLocaleTimeString(),
                `at ${specificDate.toLocaleTimeString()}`);
            const dateEnUsSpecificDatePluralHour = new DateTimeTestData(
                englishUsCulture,
                specificDatePluralHour,
                "Friday, April 04",
                "Friday, April 04",
                specificDatePluralHour.toLocaleTimeString(),
                `at ${specificDatePluralHour.toLocaleTimeString()}`);

            // Spanish from Spain (uses 24 hr format)
            const dateEsEsToday = new DateTimeTestData(
                spanishSpainCulture,
                today,
                "hoy",
                "hoy",
                `${today.toLocaleTimeString()}`,
                `a las ${today.toLocaleTimeString()}`
            );
            const dateEsEsTomorrow = new DateTimeTestData(
                spanishSpainCulture,
                tomorrow,
                "mañana",
                "mañana",
                `${tomorrow.toLocaleTimeString()}`,
                `a las ${tomorrow.toLocaleTimeString()}`
            );
            const dateEsEsSpecificDate = new DateTimeTestData(
                spanishSpainCulture,
                specificDate,
                "viernes 04 de Abril",
                "el viernes 04 de Abril",
                `${specificDate.toLocaleTimeString()}`,
                `a la ${specificDate.toLocaleTimeString()}`
            );
            const dateEsEsSpecificDatePluralHour = new DateTimeTestData(
                spanishSpainCulture,
                specificDatePluralHour,
                "viernes 04 de Abril",
                "el viernes 04 de Abril",
                `${specificDatePluralHour.toLocaleTimeString()}`,
                `a las ${specificDatePluralHour.toLocaleTimeString()}`
            );
            
            // Spanish from Mexico (uses AM PM)
            const dateEsMxSpecificDate = new DateTimeTestData(
                spanishMexicoCulture,
                specificDate,
                "viernes 04 de Abril",
                "el viernes 04 de Abril",
                `${specificDate.toLocaleTimeString()}`,
                `a la ${specificDate.toLocaleTimeString()}`
            );
            const dateEsMxSpecificDatePluralHour = new DateTimeTestData(
                spanishMexicoCulture,
                specificDatePluralHour,
                "viernes 04 de Abril",
                "el viernes 04 de Abril",
                `${specificDatePluralHour.toLocaleTimeString()}`,
                `a las ${specificDatePluralHour.toLocaleTimeString()}`
            );

            testData.push(dateEnUsToday);
            testData.push(dateEnUsTomorrow);
            testData.push(dateEnUsSpecificDate);
            testData.push(dateEnUsSpecificDatePluralHour);
            testData.push(dateEsEsToday);
            testData.push(dateEsEsTomorrow);
            testData.push(dateEsEsSpecificDate);
            testData.push(dateEsEsSpecificDatePluralHour);
            testData.push(dateEsMxSpecificDate);
            testData.push(dateEsMxSpecificDatePluralHour);
            
            testData.forEach(data => {
                it(`should resolve ${data.expectedDateSpeech} ${data.expectedTimeSpeech} in ${data.culture}`, async function() {
                    const locale = data.culture.substring(0, 2);
                    strictEqual(data.expectedDateSpeech, await DateTimeEx.toSpeechDateString(data.inputDateTime, locale));
                    strictEqual(data.expectedDateSpeechWithSuffix, await DateTimeEx.toSpeechDateString(data.inputDateTime, locale, true));
                    strictEqual(data.expectedTimeSpeech , DateTimeEx.toSpeechTimeString(data.inputDateTime, locale));
                    strictEqual(data.expectedTimeSpeechWithSuffix, DateTimeEx.toSpeechTimeString(data.inputDateTime, locale, true));
                });
            });
        });
    });

    describe("using resource values", function() {
        describe("validate the outputs for each culture", function(){
            const cultures = [
                "en-US",
                "es-ES",
                "es-MX",
                "de-DE",
                "it",
                "zh",
                "fr"
            ];
            cultures.forEach(culture => {
                it(`should resolve today and tomorrow in ${culture}`, async function() {
                    const locale = culture.substring(0, 2);
                    const today = new Date();
                    const tomorrow = new Date();
                    tomorrow.setDate(today.getDate() + 1);
                    const otherDate = new Date();
                    otherDate.setDate(today.getDate() + 3);
                    const jsonPath = ResponsesUtil.getResourcePath(CommonResponses.name, CommonResponses.pathToResource, locale);
                    const commonFile = readFileSync(jsonPath, 'utf8');

                    strictEqual(JSON.parse(commonFile)["today"], await DateTimeEx.toSpeechDateString(today, locale));
                    strictEqual(JSON.parse(commonFile)["tomorrow"], await DateTimeEx.toSpeechDateString(tomorrow, locale));
                    strictEqual(dayjs(otherDate).locale(locale).format(JSON.parse(commonFile)["spokenDateFormat"]), await DateTimeEx.toSpeechDateString(otherDate, locale));
                    if(JSON.parse(commonFile)["spokenDatePrefix"] === ""){
                        strictEqual(`${dayjs(otherDate).locale(locale).format(JSON.parse(commonFile)["spokenDateFormat"])}`, await DateTimeEx.toSpeechDateString(otherDate, locale, true));
                    } else {
                        strictEqual(`${JSON.parse(commonFile)["spokenDatePrefix"]} ${dayjs(otherDate).locale(locale).format(JSON.parse(commonFile)["spokenDateFormat"])}`, await DateTimeEx.toSpeechDateString(otherDate, locale, true));
                    }
                });
            });
        });
    });
});
