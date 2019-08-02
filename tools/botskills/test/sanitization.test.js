/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { sanitizePath } = require("../lib/utils");

describe("The sanitization path util", function () {
    describe("should return a path without trailing backslash", function () {
        it("when a path does not contain a trailing backslash", async function() {
            const path = "this\\is\my\\path";
            strictEqual(sanitizePath(path), path);
        });
        
        it("when a path contains a trailing backslash", async function() {
            const path = "this\\is\my\\path\\";
            strictEqual(sanitizePath(path), path.substring(0, path.length - 1));
        });
    })
})