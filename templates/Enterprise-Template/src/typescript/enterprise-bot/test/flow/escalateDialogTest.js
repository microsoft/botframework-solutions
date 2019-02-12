const enterpriseBotTestBase = require('./enterpriseBotTestBase.js');
const testNock = require('../testBase');

describe("Escalate Dialog", function () {
    beforeEach(function () {
        enterpriseBotTestBase.initialize();
    });

    describe("Escalating", function () {
        it("Send 'I want to talk to a human' and check you get the expected response", function (done) {
            const testAdapter = enterpriseBotTestBase.getTestAdapter();
            const flow = testAdapter
                .send('I want to talk to a human')
                .assertReply('Our agents are available 24/7 at 1(800)555-1234.');

            testNock.resolveWithMocks(this.test.title, done, flow);
        });
    });
});
