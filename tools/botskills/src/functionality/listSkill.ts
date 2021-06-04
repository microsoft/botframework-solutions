/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync } from 'fs';
import { ConsoleLogger, ILogger} from '../logger';
import { IListConfiguration, IAppSetting, ISkill } from '../models';
import { EOL } from 'os';
import { sanitizeAppSettingsProperties } from '../utils';

export class ListSkill {
    public logger: ILogger;
    public constructor(logger?: ILogger) {
        this.logger = logger || new ConsoleLogger();
    }
    public async listSkill(configuration: IListConfiguration): Promise<boolean> {
        try {
            // Validate configuration.appSettingsFile
            if (!existsSync(configuration.appSettingsFile)) {
                this.logger.error(`The 'appSettingsFile' argument is absent or leads to a non-existing file.${
                    EOL }Please make sure to provide a valid path to your Assistant Skills configuration file using the '--appSettingsFile' argument.`);

                return false;
            }
            // Take VA Skills configurations
            const assistantAppSettingsFile: IAppSetting = JSON.parse(sanitizeAppSettingsProperties(configuration.appSettingsFile));
            if (assistantAppSettingsFile.botFrameworkSkills === undefined) {
                this.logger.message('There are no Skills connected to the assistant.');

                return false;
            }
            const assistantSkills: ISkill[] = assistantAppSettingsFile.botFrameworkSkills;

            if (assistantSkills.length < 1) {
                this.logger.message('There are no Skills connected to the assistant.');

                return false;
            } else {
                let message = `The skills already connected to the assistant are the following:`;
                assistantSkills.forEach((skillManifest: ISkill): void => {
                    message += `${ EOL }\t- ${ skillManifest.id }`;
                });

                this.logger.message(message);
            }

            return true;
        } catch (err) {
            this.logger.error(`There was an error while listing the Skills connected to your assistant:${ EOL + err }`);

            return false;
        }
    }
}
