/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

class TestLogger {
    constructor() {
        this._message = '';
        this._error = '';
        this._success = '';
        this._warning = '';
        this._command = '';
        this._isError = false;
        this._isVerbose = false;
    }
    error(message) {
        this._error = message;
        this._isError = true;
    }
    message(message) {
        this._message = message;
    }
    success(message) {
        this._success = message;
    }
    warning(message) {
        this._warning = message;
    }
    command(message, command) {
        if (this.isVerbose) {
            this._command = command;
        }
        else {
            this.message(message);
        }
    }
    get isVerbose() { return this._isVerbose; }
    set isVerbose(value) { this._isVerbose = value || false; }
    isError() { return this._isError; }
    getMessage() { return this._message; }
    getError() { return this._error; }
    getSuccess() { return this._success; }
    getWarning() { return this._warning; }
    getCommand() { return this._command; }
}

exports.TestLogger = TestLogger;
