/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

// DO NOT MODIFY THIS CODE
// This script is run as part of the Post Deploy step when
// deploying the bot to Azure.  It ensures the Azure Web App
// is configured correctly to host a TypeScript authored bot.
const fs = require("fs");
const path = require("path");
const replace = require("replace");
const WEB_CONFIG_FILE = path.join(__dirname, "..", "web.config");

if (fs.existsSync(path.resolve(WEB_CONFIG_FILE))) {
  replace({
    regex: 'url="index.js"',
    replacement: 'url="lib/index.js"',
    paths: [WEB_CONFIG_FILE],
    recursive: false,
    silent: true
  });
}
