/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const TEST_MODE = "lockdown";
const join = require("path").join;

const nockBack = require("nock").back;
nockBack.setMode(TEST_MODE);
nockBack.fixtures = join(__dirname, '..', 'mocks', 'nockFixtures');

const uuidRegex = /[a-f\d-]{8}-[a-f\d-]{4}-[a-f\d-]{4}-[a-f\d-]{4}-[a-f\d-]{12}/;
const qnaRegex = /\/\/[^.]+\.azurewebsites/;

const replaceUUID = function(value) {
  return value.replace(uuidRegex, "f7c2ee78-8679-4a3e-b384-0cd10c67e554");
};

const replaceScope = function(value) {
  return value.replace(qnaRegex, "//skill-hostname-qnahost.azurewebsites");
};

const beforeNock = function(scope) {
  scope.filteringRequestBody = function(body, rBody) {
    if (body === JSON.stringify(rBody)) {
      return JSON.parse(body);
    }

    return body;
  };

  scope.filteringPath = replaceUUID;

  scope.filteringScope = replaceScope;
};

const afterRecordNock = function(scopes) {
  return scopes.map(function(scope) {
    scope.path = replaceUUID(scope.path);
    scope.scope = replaceScope(scope.scope);

    return scope;
  });
};

const resolveWithMocks = function(testName, done, testFlow) {
  nockBack(
    testName + ".json",
    { before: beforeNock, afterRecord: afterRecordNock },
    function(nockDone) {
      testFlow
        .then(function() {
          nockDone();
          done();
        })
        .catch(function(err) {
          done(err);
        });
    }
  );
};

module.exports = {
  resolveWithMocks: resolveWithMocks,
  testMode: TEST_MODE
};
