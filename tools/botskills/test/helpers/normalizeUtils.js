/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const { readFileSync } = require("fs");

/**
 * Normalize the line endings of the content received
 */
function getNormalizedFile(filePath) {
    return readFileSync(filePath, 'utf-8')
        .replace(/\r\n/gm, "\n") //normalize
        .replace(/\n/gm, "\r\n"); //CR+LF - Windows EOL;;
}

exports.getNormalizedFile = getNormalizedFile;