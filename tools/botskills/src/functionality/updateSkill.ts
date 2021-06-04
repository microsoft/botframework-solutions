/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConsoleLogger, ILogger } from '../logger';
import { IConnectConfiguration, IDisconnectConfiguration, ISkillManifestV2, ISkillManifestV1, IUpdateConfiguration, ISkill, IAppSetting } from '../models';
import { ConnectSkill } from './connectSkill';
import { DisconnectSkill } from './disconnectSkill';
import { ManifestUtils, sanitizeAppSettingsProperties } from '../utils';
import { IManifest } from '../models/manifest';
import { EOL } from 'os';

export class UpdateSkill {
    private readonly configuration: IUpdateConfiguration;
    private readonly logger: ILogger;
    private readonly manifestUtils: ManifestUtils;

    public constructor(configuration: IUpdateConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
        this.manifestUtils = new ManifestUtils();
    }

    private async existSkill(): Promise<boolean> {
        try {
            // Take skillManifest
            const rawManifest: string = await this.manifestUtils.getRawManifestFromResource(this.configuration);
            const skillManifest: IManifest = await this.manifestUtils.getManifest(rawManifest, this.logger);

            const assistantSkillsFile: IAppSetting = JSON.parse(sanitizeAppSettingsProperties(this.configuration.appSettingsFile));
            const assistantSkills: ISkill[] = assistantSkillsFile.botFrameworkSkills !== undefined ? assistantSkillsFile.botFrameworkSkills : [];
            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkill): boolean => assistantSkill.id === skillManifest.id)) {
                this.configuration.skillId = skillManifest.id;

                return true;
            }
            else {
                return false;
            }
        } catch (err) {
            throw err;
        }
    }

    private async executeDisconnectSkill(): Promise<void> {
        const disconnectConfiguration: IDisconnectConfiguration = {...{}, ...this.configuration};
        disconnectConfiguration.noRefresh = true;
        await new DisconnectSkill(disconnectConfiguration).disconnectSkill();
    }

    private async executeConnectSkill(): Promise<void> {
        const connectConfiguration: IConnectConfiguration = {...{}, ...this.configuration};
        connectConfiguration.noRefresh = this.configuration.noRefresh;
        await new ConnectSkill(connectConfiguration, this.logger).connectSkill();
    }

    public async updateSkill(): Promise<boolean> {
        try {
            if (await this.existSkill()) {
                await this.executeDisconnectSkill();
                await this.executeConnectSkill();
                this.logger.success(
                    `Successfully updated '${ this.configuration.skillId }' skill from your assistant's skills configuration file.`);
            } else {
                const manifestParameter: string = this.configuration.localManifest
                    ? `--localManifest "${ this.configuration.localManifest }"`
                    : `--remoteManifest "${ this.configuration.remoteManifest }"`;
                throw new Error(`The Skill doesn't exist in the Assistant, run 'botskills connect ${ manifestParameter } --luisFolder "${ this.configuration.luisFolder }" --${ this.configuration.lgLanguage }'`);
            }

            return true;
        } catch (err) {
            this.logger.error(`There was an error while updating the Skill from the Assistant:${ EOL + err }`);

            return false;
        }
    }
}
