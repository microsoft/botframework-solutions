/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { ChildProcessUtils } = require("../lib/utils");
const childProcessUtils = new ChildProcessUtils();
const validOutput = "testing childProcess util";
const unrecognizeCommand = "unrecognizeCommand";
const { EOL, platform } = require('os');

describe("The child process util", function() {

    function platformSpecificError(command) {
        switch (platform()) {
            case 'win32':
                return `'${ command }' is not recognized as an internal or external command,${ EOL }operable program or batch file.${ EOL }`;
            case 'linux':
                return `/bin/sh: 1: ${ command }: not found${ EOL }`;
            default:
                return 'Unrecognized OS';
        }
      }

    describe("should resolve the promise", function() {
        describe("when execute the external command", function() {
            it("echo to get the validOutput value", async function() {
                const echoOutput = await childProcessUtils.execute("echo", [validOutput]);
                strictEqual(echoOutput, validOutput + EOL);
            });
        });

        describe("when try to execute the external command", function() {
            it("echo to get the validOutput value", async function() {
                const echoOutput = await childProcessUtils.tryExecute(["echo", validOutput]);
                strictEqual(echoOutput, validOutput + EOL);
            });
        });

        it("when execute any dispatch command", async function() {
            const dispatchVersionOutput = await childProcessUtils.execute("dispatch", ["-v"]);
            strictEqual(dispatchVersionOutput, ``);
        });
    });

    describe("should throw an error", function() {
        it("when execute a command which is not recognized", async function() {
            let unrecognizeCommandOutput;
            try {
                unrecognizeCommandOutput = await childProcessUtils.execute(unrecognizeCommand, ['']);
            } catch(err) {
                strictEqual(err, platformSpecificError(unrecognizeCommand));
            }
        });

        it("when try to execute a command which is not recognized", async function() {
            try {
                unrecognizeCommandOutput = await childProcessUtils.tryExecute([unrecognizeCommand]);
            } catch(err) {
                strictEqual(err, platformSpecificError(unrecognizeCommand));
            }
        });

        it("when execute a dispatch command which is not recognized", async function() {
            try {
                unrecognizeCommandOutput = await childProcessUtils.execute("dispatch", [unrecognizeCommand]);
            } catch(err) {
                strictEqual(err, EOL);
            }
        });

        it("when try to execute an empty command", async function() {
            try {
                unrecognizeCommandOutput = await childProcessUtils.tryExecute([""]);
            } catch(err) {
                strictEqual(err.message, `The argument 'file' cannot be empty. Received ''`);
            }
        });
    });
});