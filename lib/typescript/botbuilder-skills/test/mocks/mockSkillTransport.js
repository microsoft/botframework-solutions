/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

class MockSkillTransport {
    async forwardToSkill(turnContext, activity, tokenRequestHandler) {
        this.activityForwarded = activity;
        return true;
    }

    checkIfSkillInvoked(){
        return this.activityForwarded !== undefined;
    }

    verifyActivityForwardedCorrectly(activityToMatch){
        return JSON.stringify(this.activityForwarded) === JSON.stringify(activityToMatch);
    }
}

exports.MockSkillTransport = MockSkillTransport;