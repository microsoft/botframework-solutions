/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync } from 'fs';
import { isAbsolute, join, resolve } from 'path';
import { get } from 'request-promise-native';
import { ConsoleLogger, ILogger } from '../logger';
import { IConnectConfiguration, IDisconnectConfiguration, ISkillManifestV2, ISkillManifestV1, IUpdateConfiguration, ISkill, IAppSetting } from '../models';
import { ConnectSkill } from './connectSkill';
import { DisconnectSkill } from './disconnectSkill';
import { manifestV1Validation, manifestV2Validation } from '../utils';

enum manifestVersion {
    V1 = 'V1',
    V2 = 'V2',
    none = 'none'
}

export class UpdateSkill {
    private readonly configuration: IUpdateConfiguration;
    private readonly logger: ILogger;

    public constructor(configuration: IUpdateConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
    }

    private async getRemoteManifest(manifestUrl: string): Promise<ISkillManifestV1 | ISkillManifestV2> {
        try {
            return get({
                uri: manifestUrl,
                json: true
            });
        } catch (err) {
            throw new Error(`There was a problem while getting the remote manifest:\n${ err }`);
        }
    }

    private getLocalManifest(manifestPath: string): ISkillManifestV1 | ISkillManifestV2 {
        const skillManifestPath: string = isAbsolute(manifestPath) ? manifestPath : join(resolve('./'), manifestPath);

        if (!existsSync(skillManifestPath)) {
            throw new Error(`The 'localManifest' argument leads to a non-existing file.
Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        }

        return JSON.parse(readFileSync(skillManifestPath, 'UTF8'));
    }

    private validateManifestSchema(skillManifest: ISkillManifestV1 | ISkillManifestV2): manifestVersion {

        const skillManifestV1Validation = skillManifest as ISkillManifestV1;
        const skillManifestV2Validation = skillManifest as ISkillManifestV2;

        const skillManifestVersion: string | undefined = skillManifestV1Validation.id ? 
            manifestVersion.V1 : skillManifestV2Validation.$id ?
                manifestVersion.V2 : undefined;
        
        let validVersion: manifestVersion = manifestVersion.none;
        switch (skillManifestVersion) {
            case manifestVersion.V1: {
                manifestV1Validation(skillManifest as ISkillManifestV1, this.logger);
                if (!this.logger.isError)
                {
                    validVersion = manifestVersion.V1;
                    break;
                }
                throw new Error('Your Skill Manifest is not compatible. Please note that the minimum supported manifest version is 2.1.');
            }
            case manifestVersion.V2: {
                manifestV2Validation(skillManifest as ISkillManifestV2, this.logger, this.configuration.endpointName);
                if (!this.logger.isError)
                {
                    validVersion = manifestVersion.V2;
                    break;
                }
                throw new Error('Your Skill Manifest is not compatible. Please note that the minimum supported manifest version is 2.1.');
            }
            case undefined: {
                throw new Error('Your Skill Manifest is not compatible. Please note that the minimum supported manifest version is 2.1.');
            }
        }

        return validVersion;
        
    }

    private async getManifest(): Promise<ISkillManifestV1 | ISkillManifestV2> {

        return this.configuration.localManifest
            ? this.getLocalManifest(this.configuration.localManifest)
            : this.getRemoteManifest(this.configuration.remoteManifest);
    }

    private async existSkill(): Promise<boolean> {
        try {
            // Take skillManifest
            const skillManifest: ISkillManifestV1 | ISkillManifestV2 = await this.getManifest();

            // Manifest schema validation
            const validVersion: manifestVersion = this.validateManifestSchema(skillManifest);

            let skillId = '';
            if(validVersion === manifestVersion.V1) {
                const skillManifestV1 = skillManifest as ISkillManifestV1;
                skillId = skillManifestV1.id;
            } else if (validVersion === manifestVersion.V2) {
                const skillManifestV2 = skillManifest as ISkillManifestV2;
                skillId = skillManifestV2.$id;
            } else {
                return false;
            }

            const assistantSkillsFile: IAppSetting = JSON.parse(readFileSync(this.configuration.appSettingsFile, 'UTF8'));
            const assistantSkills: ISkill[] = assistantSkillsFile.BotFrameworkSkills !== undefined ? assistantSkillsFile.BotFrameworkSkills : [];
            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkill): boolean => assistantSkill.Id === skillId)) {
                this.configuration.skillId = skillId;

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
            this.logger.error(`There was an error while updating the Skill from the Assistant:\n${ err }`);

            return false;
        }
    }
}
