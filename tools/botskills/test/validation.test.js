/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join, resolve } = require("path");
const { validatePairOfArgs, manifestV1Validation, manifestV2Validation } = require("../lib/utils");
const { getNormalizedFile } = require("./helpers/normalizeUtils");
const testLogger = require("./helpers/testLogger");
const invalidManifestV1 = JSON.parse(getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v1", "invalidIdManifest.json"))));
const connectableManifestV1 = JSON.parse(getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v1", "connectableManifest.json"))));
let emptyManifestV2 = JSON.parse(getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v2", "invalidManifest.json"))));
let manifestV2 = JSON.parse(getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json"))));

function undoChangesInTemporalObjects() {
    emptyManifestV2 = JSON.parse(getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v2", "invalidManifest.json"))));
    manifestV2 = JSON.parse(getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json"))));
}

describe("The validation util", function() {
    beforeEach(function() {
        undoChangesInTemporalObjects();
        this.logger = new testLogger.TestLogger();
    });

    describe("should return an error", function() {
        it("when the manifest v1 is missing all mandatory fields", function() {
            manifestV1Validation({ }, this.logger);
            const errorMessages = [
                `Missing property 'name' of the manifest`,
                `Missing property 'id' of the manifest`,
                `Missing property 'endpoint' of the manifest`,
                `Missing property 'authenticationConnections' of the manifest`,
                `Missing property 'actions' of the manifest`
            ]

            const errorList = this.logger.getError();            
			errorList.forEach((errorMessage, index) => {
                strictEqual(errorMessage, errorMessages[index]);
            });
        });

        it("when the manifest v1 has an id with invalid characters", function() {
            manifestV1Validation(invalidManifestV1, this.logger);
            const error = this.logger.getError();            

            strictEqual(error[0], "The 'id' of the manifest contains some characters not allowed. Make sure the 'id' contains only letters, numbers and underscores, but doesn't start with number.");
        });

        it("when the manifest v1 has an invalid endpoint", function() {
            connectableManifestV1.endpoint = "invalid_endpoint";
            manifestV1Validation(connectableManifestV1, this.logger);
            const error = this.logger.getError();            

            strictEqual(error[0], "The 'endpoint' property contains some characters not allowed.");
        });

        it("when the manifest v2 is missing all mandatory fields", function() {
            manifestV2Validation(emptyManifestV2, this.logger);
            const errorMessages = [
                `Missing property '$schema' of the manifest`,
                `Missing property '$id' of the manifest`,
                `Missing property 'endpoints' of the manifest`,
                `Missing property 'dispatchModels' of the manifest`,
                `Missing property 'activities' of the manifest`
            ]

            const errorList = this.logger.getError();            
			errorList.forEach((errorMessage, index) => {
                strictEqual(errorMessage, errorMessages[index]);
            });
        });

        it("when the manifest v2 has an id with invalid characters", function() {
            manifestV2.$id = invalidManifestV1.id;
            
            manifestV2Validation(manifestV2, this.logger);
            const error = this.logger.getError();            

            strictEqual(error[0], "The '$id' of the manifest contains some characters not allowed. Make sure the '$id' contains only letters, numbers and underscores, but doesn't start with number.");
        });

        it("when the manifest v2 has an endpoint with all the mandatory fields missing", function() {
            manifestV2.endpoints[0].name = "";
            manifestV2.endpoints[0].msAppId = "";
            manifestV2.endpoints[0].endpointUrl = "";

            manifestV2Validation(manifestV2, this.logger);
            const errorMessages = [
                `Missing property 'name' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`,
                `Missing property 'msAppId' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`,
                `Missing property 'endpointUrl' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`
            ]

            const errorList = this.logger.getError();            
			errorList.forEach((errorMessage, index) => {
                strictEqual(errorMessage, errorMessages[index]);
            });
        });

        it("when the manifest v2 has an endpoint with invalid msAppId", function() {
            manifestV2.endpoints[0].msAppId = "00000000-0000-GGGG-0000-000000000000";
            
            manifestV2Validation(manifestV2, this.logger);
            const error = this.logger.getError();            

            strictEqual(error[0], "The 'msAppId' property of the selected endpoint contains invalid characters or does not comply with the GUID format. If you didn't select any endpoint, the first one is taken by default.");
        });

        it("when the manifest v2 has an endpoint with invalid url", function() {
            manifestV2.endpoints[0].endpointUrl = "invalid_endpoint";
            
            manifestV2Validation(manifestV2, this.logger);
            const error = this.logger.getError();            

            strictEqual(error[0], "The 'endpointUrl' property of the selected endpoint contains invalid characters or does not comply with the URL format. If you didn't select any endpoint, the first one is taken by default.");
        });
    });

    describe("should return a message", function() {
        it("that one of both arguments is necessary", function() {
            const message = validatePairOfArgs(undefined, undefined);
            strictEqual(message, "One of the arguments '{0}' or '{1}' should be provided.");
        });

        it("that only one argument is necessary", function() {
            const message = validatePairOfArgs("val1", "val2");
            strictEqual(message, "Only one of the arguments '{0}' or '{1}' should be provided.");
        });
    });


    describe("should return an empty string", function() {
        it("when only one argument is send", function() {
            const messageFirstArg = validatePairOfArgs("val1", undefined);
            strictEqual(messageFirstArg, "");
            const messageSecondArg = validatePairOfArgs(undefined, "val2");
            strictEqual(messageSecondArg, "");
            strictEqual(messageFirstArg, messageSecondArg);
        });
    });
});