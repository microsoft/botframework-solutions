/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import { SkillValidation, ClaimsIdentity } from 'botframework-connector';

export namespace TurnContextEx {

    export function isSkill(turnContext: TurnContext): boolean {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const botIdentity = turnContext.turnState.get(turnContext.adapter.BotIdentityKey);
        return botIdentity instanceof ClaimsIdentity && SkillValidation.isSkillClaim(botIdentity.claims) ? true : false;
    }

}