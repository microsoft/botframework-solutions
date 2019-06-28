/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

 import { existsSync, readFileSync } from 'fs';
 import { isAbsolute, join, resolve } from 'path';
 import { get } from 'request-promise-native';
 import { ConsoleLogger, ILogger } from '../logger';
 import { IConnectConfiguration, IDisconnectConfiguration, ISkillFile, ISkillManifest, IUpdateConfiguration } from '../models';
 import { ConnectSkill } from './connectSkill';
 import { DisconnectSkill } from './disconnectSkill';

 export class UpdateSkill {
    public logger: ILogger;
    private connectSkill: ConnectSkill;
    private disconnectSkill: DisconnectSkill;

    constructor(logger?: ILogger) {
        this.logger = logger || new ConsoleLogger();
        this.connectSkill = new ConnectSkill(this.logger);
        this.disconnectSkill = new DisconnectSkill(this.logger);
    }

    private async getRemoteManifest(manifestUrl: string): Promise<ISkillManifest> {
        try {
            return get({
                uri: <string> manifestUrl,
                json: true
            });
        } catch (err) {
            throw new Error(`There was a problem while getting the remote manifest:\n${err}`);
        }
    }

    private getLocalManifest(manifestPath: string): ISkillManifest {
        const skillManifestPath: string = isAbsolute(manifestPath) ? manifestPath : join(resolve('./'), manifestPath);

        if (!existsSync(skillManifestPath)) {
            throw new Error(`The 'localManifest' argument leads to a non-existing file.
Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        }

        return JSON.parse(readFileSync(skillManifestPath, 'UTF8'));
    }

    private async existSkill(configuration: IUpdateConfiguration): Promise<boolean> {
        try {
            // Take skillManifest
            const skillManifest: ISkillManifest = configuration.localManifest
            ? this.getLocalManifest(configuration.localManifest)
            : await this.getRemoteManifest(configuration.remoteManifest);
            const assistantSkillsFile: ISkillFile = JSON.parse(readFileSync(configuration.skillsFile, 'UTF8'));
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills || [];
            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkillManifest) => assistantSkill.id === skillManifest.id)) {
                configuration.skillId = skillManifest.id;

                return true;
            }

            return false;
        } catch (err) {
            throw err;
        }
    }

    public async updateSkill(configuration: IUpdateConfiguration): Promise<boolean> {
        try {
            if (await this.existSkill(configuration)) {
                const disconnectConfiguration: IDisconnectConfiguration = {...{}, ...configuration};
                disconnectConfiguration.noRefresh = true;
                await this.disconnectSkill.disconnectSkill(disconnectConfiguration);
                const connectConfiguration: IConnectConfiguration = {...{}, ...configuration};
                connectConfiguration.noRefresh = configuration.noRefresh;
                await this.connectSkill.connectSkill(connectConfiguration);
                this.logger.success(
                    `Successfully updated '${configuration.skillId}' skill from your assistant's skills configuration file.`);
            } else {
                const manifestParameter: string = configuration.localManifest
                ? `--localManifest "${configuration.localManifest}"`
                : `--remoteManifest "${configuration.remoteManifest}"`;
                // tslint:disable: max-line-length
                throw new Error(`The Skill doesn't exist in the Assistant, run 'botskills connect --botName ${configuration.botName} ${manifestParameter} --luisFolder "${configuration.luisFolder}" --${configuration.lgLanguage}'`);
            }

            return true;
        } catch (err) {
            this.logger.error(`There was an error while updating the Skill from the Assistant:\n${err}`);

            return false;
        }
    }
 }
