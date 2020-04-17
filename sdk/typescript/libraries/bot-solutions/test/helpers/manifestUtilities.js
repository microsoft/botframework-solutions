/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const createSkill = function (id, name, endpoint, actionId, slots) {
    const skillManifest = {
        name: name,
        id: id,
        endpoint: endpoint,
        actions: []
    }

    const action = {
        id: actionId,
        definition: {
            slots: []
        }
    }

    // Provide slots if we have them
    if (slots !== undefined) {
        action.definition.slots = slots;
    }

    skillManifest.actions.push(action);

    return skillManifest;
}

const createAction = function (id, slots) {
    const action = {
        id: id,
        definition: {
            slots: slots
        }
    }

    return action;
}

module.exports = {
    createAction: createAction,
    createSkill: createSkill
};