/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { SkillDialog } = require('../../lib/skillDialog');

class SkillDialogTest extends SkillDialog {
    constructor(skillManifest, appCredentials, telemetryClient, skillContextAccessor, skillTransport){
        super(skillManifest, appCredentials, telemetryClient, skillContextAccessor, undefined, undefined, skillTransport);
    }
}

exports.SkillDialogTest = SkillDialogTest;
