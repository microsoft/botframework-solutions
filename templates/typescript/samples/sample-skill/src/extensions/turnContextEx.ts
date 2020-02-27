/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import { SkillValidation, ClaimsIdentity } from 'botframework-connector';

export namespace TurnContextEx {

    export function isSkill(turnContext: TurnContext): boolean {
        const botIdentity = turnContext.turnState.get('BotIdentity');
        return botIdentity instanceof ClaimsIdentity && SkillValidation.isSkillClaim(botIdentity.claims) ? true : false;
    }

}