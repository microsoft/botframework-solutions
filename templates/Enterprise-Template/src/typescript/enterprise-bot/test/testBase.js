const snakeCase = require('lodash').snakeCase;

const TEST_MODE = 'lockdown';

const nockBack = require('nock').back;
nockBack.setMode(TEST_MODE);
nockBack.fixtures = __dirname + '/nockFixtures';

const beforeNock = function (scope) {
    scope.filteringRequestBody = function (body, rBody) {
        if (body === JSON.stringify(rBody)) {
            return JSON.parse(body);
        }

        return body;
    }
};

const resolveWithMocks = function (testName, done, testFlow) {
    nockBack(snakeCase(testName) + '.json', { before: beforeNock }, function (nockDone) {
        testFlow
        .then(function () {
            nockDone();
            done();
        })
        .catch(function (err) {
            done(err);
        });
    });
}

module.exports = {
    resolveWithMocks: resolveWithMocks,
    testMode: TEST_MODE
}
