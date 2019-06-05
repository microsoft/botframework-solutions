const { join } = require('path');
const { back } = require('nock');

back.setMode('record');
back.fixtures = join(__dirname, '..', 'fixtures');

function resolvePromise(testFlow) {
    if (typeof testFlow === 'function') {
        return Promise.resolve(testFlow());
    }

    return Promise.resolve(testFlow);
}

function withNock(testName, done, test) {
    back(`${testName}.json`, function(nockDone) {
        resolvePromise(test)
        .then(nockDone)
        .then(done)
        .catch(done);
    });
}

exports.withNock = withNock;
