/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { validatePairOfArgs } = require("../lib/utils");

describe("The validation util", function() {
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