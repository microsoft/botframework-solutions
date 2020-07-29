/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

/**
 * Possible value: record | lockdown
 */
const TEST_MODE = 'lockdown';
const join = require('path').join;

const nockBack = require('nock').back;
nockBack.setMode(TEST_MODE);
nockBack.fixtures = join(__dirname, '..', 'mocks', 'nockFixtures');

const uuidRegex = /[a-f\d-]{8}-[a-f\d-]{4}-[a-f\d-]{4}-[a-f\d-]{4}-[a-f\d-]{12}/;

function sanitizeUUID(value) {
    return value.replace(uuidRegex, 'f7c2ee78-8679-4a3e-b384-0cd10c67e554');
}

function beforeNock(scope) {
    // Fix issue with LUIS responses as JSON strings
    scope.filteringRequestBody = function (body, rBody) {
        if (body === JSON.stringify(rBody)) {
            return JSON.parse(body);
        }

        return body;
    }
    // filter keys
    scope.filteringPath = sanitizeUUID;
    scope.filteringScope = sanitizeUUID;
}

function afterRecordNock(scopes) {
    return scopes.map(function (scope) {
        scope.path = sanitizeUUID(scope.path)
        scope.scope = sanitizeUUID(scope.scope);

        return scope;
    });
}

const nockSettings = { before: beforeNock, afterRecord: afterRecordNock };

function resolveWithMocks(testName, done, testFlow) {
    nockBack(testName + '.json', nockSettings, function (nockDone) {
        testFlow.then(function () {
            nockDone();
            done();
        }).catch(function (err) {
            done(err);
        });
    });
}

function simpleMock(testName, done, testFlow) {
    nockBack(`${testName}.json`, function(nockDone) {
        testFlow.then(function() {
            nockDone();
            done();
        }).catch(function(err) {
            done(err);
        });
    });
}

module.exports = {
    resolveWithMocks: resolveWithMocks,
    simpleMock: simpleMock,
    testMode: TEST_MODE
}
