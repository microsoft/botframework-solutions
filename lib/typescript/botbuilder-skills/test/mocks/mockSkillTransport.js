/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

class MockSkillTransport {
    async forwardToSkill(turnContext, activity, tokenRequestHandler) {
        this.activityForwarded = activity;
        return Promise.resolve(true);
    }

    checkIfSkillInvoked() {
        return this.activityForwarded !== undefined;
    }

    verifyActivityForwardedCorrectly(activityAssertion){
        activityAssertion(this.activityForwarded);
    }
}

exports.MockSkillTransport = MockSkillTransport;