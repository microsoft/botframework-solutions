/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { ok } = require("assert");
const { ConsoleLogger } = require("../lib/logger");
const sandbox = require("sinon").createSandbox();
const logger = new ConsoleLogger();
const message = "Custom Message";
const command = "Custom Command";

describe("The logger", function () {
    beforeEach(function () {
        this.stubError = sandbox.spy(console, 'error');
        this.stubMessage = sandbox.spy(console, 'log');
    });

    afterEach(function() {
        this.stubError.restore();
        this.stubMessage.restore();
    });

    describe("should print", function(){
        it("the custom error message", function() {
            logger.error(message);
            ok(this.stubError.called);
            ok(logger.isError);
        });

        it("the custom informative message", function() {
            logger.message(message);
            ok(this.stubMessage.called);
        });

        it("the custom success message", function() {
            logger.success(message);
            ok(this.stubMessage.called);
        });

        it("the custom warning message", function() {
            logger.warning(message);
            ok(this.stubMessage.called);
        });

        describe("the custom command message without verbose flag", function() {
            it("without verbose flag", function() {
                logger.isVerbose = undefined;
                logger.command(message, command);
                ok(this.stubMessage.called);
            });

            it("with verbose flag", function() {
                logger.isVerbose = true;
                logger.command(message, command);
                ok(this.stubMessage.called);
            });
        });
    });
});
