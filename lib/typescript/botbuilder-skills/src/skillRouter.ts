/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ISkillManifest } from './models';

/**
 * Skill Router class that helps Bots identify if a registered Skill matches the identified dispatch intent.
 */
export namespace SkillRouter {
    /**
     * Helper method to go through a SkillManifest and match the passed dispatch intent to a registered action.
     * @param skillConfiguration The Skill Configuration for the current Bot.
     * @param dispatchIntent The Dispatch intent to try and match to a skill.
     * @returns Whether the intent matches a Skill.
     */
    export function isSkill(skillConfiguration: ISkillManifest[], dispatchIntent: string): ISkillManifest | undefined {
        return skillConfiguration.find((skill: ISkillManifest) => skill.id === dispatchIntent);
    }
}
