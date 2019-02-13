const enterpriseBotTestBase = require('./enterpriseBotTestBase.js');

describe("Onboarding Dialog", function() {
    beforeEach(async function() {
        await enterpriseBotTestBase.initialize();
    });

    describe("Onboarding", function() { 
        xit("In the test spin up the OnboardingDialog directly", function(done) {
        });

        xit("Response for each prompt", function(done) {
        });

        xit("Validate state is updated", function(done) {
        });
    });

    describe("Onboarding Cancellation", function() {
        xit("Same as above but say 'Cancel' half-way through", function(done) {
        });

        xit("Validate confirmation prompt", function(done) {
        });

        xit("Send Yes", function(done) {
        });
        
        xit("Validate you go back", function(done) {
        });
     });
});
