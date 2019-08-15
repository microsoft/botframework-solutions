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
    private readonly configuration: IUpdateConfiguration;
    private readonly logger: ILogger;

    public constructor(configuration: IUpdateConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
    }

    private async getRemoteManifest(manifestUrl: string): Promise<ISkillManifest> {
        try {
            return get({
                uri: manifestUrl,
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

        // eslint-disable-next-line @typescript-eslint/tslint/config
        return JSON.parse(readFileSync(skillManifestPath, 'UTF8'));
    }

    private async existSkill(): Promise<boolean> {
        try {
            // Take skillManifest
            const skillManifest: ISkillManifest = this.configuration.localManifest
                ? this.getLocalManifest(this.configuration.localManifest)
                : await this.getRemoteManifest(this.configuration.remoteManifest);
            // eslint-disable-next-line @typescript-eslint/tslint/config
            const assistantSkillsFile: ISkillFile = JSON.parse(readFileSync(this.configuration.skillsFile, 'UTF8'));
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills !== undefined ? assistantSkillsFile.skills : [];
            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkillManifest): boolean => assistantSkill.id === skillManifest.id)) {
                this.configuration.skillId = skillManifest.id;

                return true;
            }

            return false;
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
                    `Successfully updated '${this.configuration.skillId}' skill from your assistant's skills configuration file.`);
            } else {
                const manifestParameter: string = this.configuration.localManifest
                    ? `--localManifest "${this.configuration.localManifest}"`
                    : `--remoteManifest "${this.configuration.remoteManifest}"`;
                // tslint:disable-next-line: max-line-length
                throw new Error(`The Skill doesn't exist in the Assistant, run 'botskills connect --botName ${this.configuration.botName} ${manifestParameter} --luisFolder "${this.configuration.luisFolder}" --${this.configuration.lgLanguage}'`);
            }

            return true;
        } catch (err) {
            this.logger.error(`There was an error while updating the Skill from the Assistant:\n${err}`);

            return false;
        }
    }
}
