/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * Normalize the line endings of the content received
 */
function normalizeContent(content) {
    return content.replace(/\r\n/gm, "\n") //normalize
                  .replace(/\n/gm, "\r\n"); //CR+LF - Windows EOL;
}

exports.normalizeContent = normalizeContent;