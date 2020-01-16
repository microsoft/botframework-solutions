/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { SkillDialog } = require('../../lib/skills/skillDialog');

// Extended implementation of SkillDialog for test purposes that enables us to mock the HttpClient
class SkillDialogTest extends SkillDialog {
    constructor(skillManifest, appCredentials, telemetryClient, skillContextAccessor, skillTransport){
        super(skillManifest, appCredentials, telemetryClient, skillContextAccessor, undefined, skillTransport);
    }
}

exports.SkillDialogTest = SkillDialogTest;
