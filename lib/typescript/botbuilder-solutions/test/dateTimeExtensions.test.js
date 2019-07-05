/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

// tslint:disable
require("dayjs/locale/de");
require("dayjs/locale/es");
require("dayjs/locale/it");
require("dayjs/locale/fr");
require("../lib/resources/customizeLocale/zh");
// tslint:enable
const { strictEqual } = require("assert");
const dayjs = require("dayjs");
const { join } = require("path");
const i18next = require("i18next").default;
const i18nextNodeFsBackend = require("i18next-node-fs-backend");
const { DateTimeTestData } = require("./helpers/dateTimeTestData");
const { Locales } = require(join("..", "lib", "localesUtils"));
const { DateTimeExtensions } = require(join("..", "lib", "extensions", "dateTimeExtensions"));

describe("date time extensions", function() {

    before(async function() {
        // Configure internationalization and default locale
        i18next.use(i18nextNodeFsBackend)
        .init({
            fallbackLng: "en",
            preload: [ "de", "en", "es", "fr", "it", "zh" ],
            backend: {
                loadPath: join(__dirname, "locales", "{{lng}}.json")
            }
        })
        .then(async () => {
            await Locales.addResourcesFromPath(i18next, "common");
        });
    });

    after(async function() {
        i18next.changeLanguage("en");
    });

    describe("using explicit value", function() {
        describe("validate date time extension outputs from random dates", function(){
            // Setup test data
            const today = new Date();
            const tomorrow = new Date();
            tomorrow.setDate(today.getDate() + 1);
            const specificDate = new Date("1975-04-04T01:20:42");
            const specificDatePluralHour = new Date("1975-04-04T04:30:42");
            const testData = [];

            // US English
            const dateEnUsToday = new DateTimeTestData(
                "en-US",
                today,
                "Today",
                "Today",
                today.toLocaleTimeString(),
                `at ${today.toLocaleTimeString()}`);
            const dateEnUsTomorrow = new DateTimeTestData(
                "en-US",
                tomorrow,
                "Tomorrow",
                "Tomorrow",
                tomorrow.toLocaleTimeString(),
                `at ${tomorrow.toLocaleTimeString()}`);
            const dateEnUsSpecificDate = new DateTimeTestData(
                "en-US",
                specificDate,
                "Friday, April 04",
                "Friday, April 04",
                specificDate.toLocaleTimeString(),
                `at ${specificDate.toLocaleTimeString()}`);
            const dateEnUsSpecificDatePluralHour = new DateTimeTestData(
                "en-US",
                specificDatePluralHour,
                "Friday, April 04",
                "Friday, April 04",
                specificDatePluralHour.toLocaleTimeString(),
                `at ${specificDatePluralHour.toLocaleTimeString()}`);

            // Spanish from Spain (uses AM PM)
            const dateEsEsToday = new DateTimeTestData(
                "es-ES",
                today,
                "hoy",
                "hoy",
                `${today.toLocaleTimeString()}`,
                `a las ${today.toLocaleTimeString()}`
            );
            const dateEsEsTomorrow = new DateTimeTestData(
                "es-ES",
                tomorrow,
                "mañana",
                "mañana",
                `${tomorrow.toLocaleTimeString()}`,
                `a las ${tomorrow.toLocaleTimeString()}`
            );
            const dateEsEsSpecificDate = new DateTimeTestData(
                "es-ES",
                specificDate,
                "viernes 04 de Abril",
                "el viernes 04 de Abril",
                `${specificDate.toLocaleTimeString()}`,
                `a la ${specificDate.toLocaleTimeString()}`
            );
            const dateEsEsSpecificDatePluralHour = new DateTimeTestData(
                "es-ES",
                specificDatePluralHour,
                "viernes 04 de Abril",
                "el viernes 04 de Abril",
                `${specificDatePluralHour.toLocaleTimeString()}`,
                `a las ${specificDatePluralHour.toLocaleTimeString()}`
            );
            
            // Spanish from Mexico (uses AM PM)
            const dateEsMxSpecificDate = new DateTimeTestData(
                "es-MX",
                specificDate,
                "viernes 04 de Abril",
                "el viernes 04 de Abril",
                `${specificDate.toLocaleTimeString()}`,
                `a la ${specificDate.toLocaleTimeString()}`
            );
            const dateEsMxSpecificDatePluralHour = new DateTimeTestData(
                "es-MX",
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
                    await i18next.changeLanguage(data.culture.substring(0, 2));
                    strictEqual(data.expectedDateSpeech, await DateTimeExtensions.toSpeechDateString(data.inputDateTime));
                    strictEqual(data.expectedDateSpeechWithSuffix, await DateTimeExtensions.toSpeechDateString(data.inputDateTime, true));
                    strictEqual(data.expectedTimeSpeech , DateTimeExtensions.toSpeechTimeString(data.inputDateTime));
                    strictEqual(data.expectedTimeSpeechWithSuffix, DateTimeExtensions.toSpeechTimeString(data.inputDateTime, true));
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
                    await i18next.changeLanguage(culture.substring(0, 2));
                    const locale = i18next.language;
                    const today = new Date();
                    const tomorrow = new Date();
                    tomorrow.setDate(today.getDate() + 1);
                    const otherDate = new Date();
                    otherDate.setDate(today.getDate() + 3);
                    
                    strictEqual(i18next.t("common:today"), await DateTimeExtensions.toSpeechDateString(today));
                    strictEqual(i18next.t("common:tomorrow"), await DateTimeExtensions.toSpeechDateString(tomorrow));
                    strictEqual(dayjs(otherDate).locale(locale).format(i18next.t("common:spokenDateFormat")), await DateTimeExtensions.toSpeechDateString(otherDate));
                    if(i18next.t("common:spokenDatePrefix") === ""){
                        strictEqual(`${dayjs(otherDate).locale(locale).format(i18next.t("common:spokenDateFormat"))}`, await DateTimeExtensions.toSpeechDateString(otherDate, true));
                    } else {
                        strictEqual(`${i18next.t("common:spokenDatePrefix")} ${dayjs(otherDate).locale(locale).format(i18next.t("common:spokenDateFormat"))}`, await DateTimeExtensions.toSpeechDateString(otherDate, true));
                    }
                });
            });
        });
    });
});
