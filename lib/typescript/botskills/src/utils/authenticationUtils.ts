/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { writeFileSync } from 'fs';
import { ConsoleLogger, ILogger } from '../logger';
import { IAuthenticationConnection, IConnectConfiguration, ISkillManifest } from '../models';
import { extractArgs, tryExecute } from './';

function createScopeManifest(scopes: string[]): IScopeManifest[] {
    /* should generate the following manifest
			 * [
			 *     {
			 *         "resourceAppId": "00000003-0000-0000-c000-000000000000",
			 *         "resourceAccess": [
			 *             {
			 *                 "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
			 *                 "type": "Scope"
			 *             },
			 *             {
			 *                 "id": "b340eb25-3456-403f-be2f-af7a0d370277",
			 *                 "type": "Scope"
			 *             },
			 *             {
			 *                 "id": "1ec239c2-d7c9-4623-a91a-a9775856bb36",
			 *                 "type": "Scope"
			 *             },
			 *             {
			 *                 "id": "ba47897c-39ec-4d83-8086-ee8256fa737d",
			 *                 "type": "Scope"
			 *             }
			 *         ]
			 *     }
			 * ]
			 */
    return [{
        resourceAppId: '00000003-0000-0000-c000-000000000000',
        resourceAccess: scopes.map((scope: string) => {
        return {
            id: scope,
            type: 'Scope'
        };
    })}];
}

interface IScopeManifest {
    resourceAppId: string;
    resourceAccess: IResourceAccess[];
}
interface IResourceAccess {
    id: string;
    // tslint:disable-next-line:no-reserved-keywords
    type: string;
}
interface IAzureAuthSetting {
    etag: string;
    id: string;
    kind: string;
    location: string;
    name: string;
    properties: {
        clientId: string;
        clientSecret: string;
        parameters: {
            key: string;
            value: string;
        }[];
        provisioningState: string;
        scopes: string;
        serviceProviderDisplayName: string;
        serviceProviderId: string;
        settingId: string;
    };
    resourceGroup: string;
    sku: string;
    tags: string;
    // tslint:disable-next-line:no-reserved-keywords
    type: string;
}
interface IAppSettingOauthConnection {
    oauthConnections: IOauthConnection[];
    microsoftAppId: string;
    microsoftAppPassword: string;
}
interface IOauthConnection {
    name: string;
    provider: string;
}

// tslint:disable-next-line:max-func-body-length export-name
export async function authenticate(configuration: IConnectConfiguration, manifest: ISkillManifest, logger: ILogger): Promise<void> {
    // configuring bot auth settings
    logger.message('Checking for authentication settings ...');
    if (manifest.authenticationConnections) {
        const aadConfig: IAuthenticationConnection | undefined = manifest.authenticationConnections.find(
            (connection: IAuthenticationConnection) => connection.serviceProviderId === 'Azure Active Directory v2');
        if (aadConfig) {
            logger.message('Configuring Azure AD connection ...');

            let connectionName: string = aadConfig.id;
            const newScopes: string[] = aadConfig.scopes.split(', ');
            let scopes: string[] = newScopes.slice(0); // creates a new array with the same values

            // check for existing aad connection
            let listAuthSettingsCmd: string = `az bot authsetting list `;
            listAuthSettingsCmd += `-n ${configuration.botName} `;
            listAuthSettingsCmd += `-g ${configuration.resourceGroup}`;

            const connectionsResult: string = await tryExecute('az', extractArgs(listAuthSettingsCmd));
            const connections: IAzureAuthSetting[] = JSON.parse(connectionsResult);
            const aadConnection: IAzureAuthSetting | undefined = connections.find(
                (connection: IAzureAuthSetting) => connection.properties.serviceProviderDisplayName === 'Azure Active Directory v2');
            if (aadConnection) {
                const settingName: string = aadConnection.name.split('/')[1];

                // Get current aad auth setting
                let showAuthSettingsCmd: string = `az bot authsetting show `;
                showAuthSettingsCmd += `-n ${configuration.botName} `;
                showAuthSettingsCmd += `-g ${configuration.resourceGroup} `;
                showAuthSettingsCmd += `-c ${settingName}`;

                const botAuthSettingResult: string = await tryExecute('az', extractArgs(showAuthSettingsCmd));
                const botAuthSetting: IAzureAuthSetting = JSON.parse(botAuthSettingResult);
                const existingScopes: string[] = botAuthSetting.properties.scopes.split(',');
                scopes = scopes.concat(existingScopes);
                connectionName = settingName;

                // delete current aad auth connection
                let deleteAuthSettingCmd: string = `az bot authsetting delete `;
                deleteAuthSettingCmd += `-n ${configuration.botName} `;
                deleteAuthSettingCmd += `-g ${configuration.resourceGroup} `;
                deleteAuthSettingCmd += `-c ${settingName}`;

                const deleteResult: string = await tryExecute('az', extractArgs(deleteAuthSettingCmd));
            }

            // update appsettings.json
            logger.message('Updating appsettings.json ...');
            // tslint:disable-next-line:non-literal-require
            const appSettings: IAppSettingOauthConnection = require(configuration.appSettingsFile);

            // check for and remove existing aad connections
            if (appSettings.oauthConnections) {
                appSettings.oauthConnections = appSettings.oauthConnections.filter(
                    (connection: IOauthConnection) => connection.provider !== 'Azure Active Directory v2');
            }

            // set or add new oauth setting
            const oauthSetting: IOauthConnection = { name: connectionName, provider: 'Azure Active Directory v2' };
            if (!appSettings.oauthConnections) {
                appSettings.oauthConnections = [oauthSetting];
            } else {
                appSettings.oauthConnections.push(oauthSetting);
            }

            // update appsettings.json
            writeFileSync(configuration.appSettingsFile, JSON.stringify(appSettings, undefined, 4));

            // Remove duplicate scopes
            scopes = [...new Set(scopes)];
            const scopeManifest: IScopeManifest[] = createScopeManifest(scopes);

            /* should generate the following manifest
			 * [
			 *     {
			 *         "resourceAppId": "00000003-0000-0000-c000-000000000000",
			 *         "resourceAccess": [
			 *             {
			 *                 "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
			 *                 "type": "Scope"
			 *             },
			 *             {
			 *                 "id": "b340eb25-3456-403f-be2f-af7a0d370277",
			 *                 "type": "Scope"
			 *             },
			 *             {
			 *                 "id": "1ec239c2-d7c9-4623-a91a-a9775856bb36",
			 *                 "type": "Scope"
			 *             },
			 *             {
			 *                 "id": "ba47897c-39ec-4d83-8086-ee8256fa737d",
			 *                 "type": "Scope"
			 *             }
			 *         ]
			 *     }
			 * ]
			 */
            // Update MSA scopes
            logger.message('Configuring MSA app scopes ...');
            let azureAppUpdateCmd: string = `az ad app update `;
            azureAppUpdateCmd += `--id ${appSettings.microsoftAppId} `;
            // logger.message(JSON.stringify(scopeManifest));
            const scopeManifestText: string = JSON.stringify(scopeManifest)
                .replace(/\"/g, '\'');
            // logger.message(scopeManifestText);
            azureAppUpdateCmd += `--required-resource-accesses "${scopeManifestText}"`;

            const errorResult: string = await tryExecute('az', extractArgs(azureAppUpdateCmd));
            /* for example az ad app update
             * --id 349c6ad0-4270-4d7d-b641-e971f64cfcd7
             * --required-resource-accesses "[{'resourceAppId':'00000003-0000-0000-c000-000000000000',
             *  'resourceAccess':[{'id':'e1fe6dd8-ba31-4d61-89e7-88639da4683d','type':'Scope'},
             *  {'id':'b340eb25-3456-403f-be2f-af7a0d370277','type':'Scope'},
             *  {'id':'1ec239c2-d7c9-4623-a91a-a9775856bb36','type':'Scope'},
             *  {'id':'ba47897c-39ec-4d83-8086-ee8256fa737d','type':'Scope'}]}]"
             */
            //  Catch error: Updates to converged applications are not allowed in this version.
            if (errorResult) {
                logger.warning('Could not configure scopes automatically.');
                // manualScopesRequired = true
            }

            logger.message('Updating bot oauth settings ...');
            /* az bot authsetting create
             * --name vasample
             * --resource-group vasample
             * --setting-name Outlook
             * --client-id "349c6ad0-4270-4d7d-b641-e971f64cfcd7"
             * --client-secret "z?{K9_.1%X1(n};bCX["
             * --service Aadv2
             * --parameters clientId="349c6ad0-4270-4d7d-b641-e971f64cfcd7" clientSecret="z?{K9_.1%X1(n};bCX[" tenantId=common
             * --provider-scope-string "User.Read, User.ReadBasic.All, Calendars.ReadWrite, People.Read"
             */
            let authSettingCmd: string = `az bot authsetting create `;
            authSettingCmd += `--name ${configuration.botName} `;
            authSettingCmd += `--resource-group ${configuration.resourceGroup} `;
            authSettingCmd += `--setting-name ${connectionName} `;
            authSettingCmd += `--client-id "${appSettings.microsoftAppId}" `;
            authSettingCmd += `--client-secret "${appSettings.microsoftAppPassword}" `;
            authSettingCmd += `--service Aadv2 `;
            authSettingCmd += `--parameters clientId="${appSettings.microsoftAppId}" `;
            authSettingCmd += `clientSecret="${appSettings.microsoftAppPassword}" tenantId=common `;
            authSettingCmd += `--provider-scope-string "${scopes.join(', ')}"`;

            await tryExecute('az', extractArgs(authSettingCmd));

            /* {
			 *   "etag": "W/\"ccdffe1611577e39d8feaad8df4b8d684/25/2019 12:44:15 PM\"",
			 *   "id": "/subscriptions/28b01fda-097c-4246-a0c8-1e1cfc302723/
             *      resourceGroups/vasample/providers/Microsoft.BotService/botServices/vasample/connections/Outlook",
			 *   "kind": null,
			 *   "location": "global",
			 *   "name": "vasample/Outlook",
			 *   "properties": {
			 *     "clientId": "349c6ad0-4270-4d7d-b641-e971f64cfcd7",
			 *     "clientSecret": "z?{K9_.1%X1(n};bCX[",
			 *     "parameters": [
			 *       {
			 *         "key": "clientId",
			 *         "value": "349c6ad0-4270-4d7d-b641-e971f64cfcd7"
			 *       },
			 *       {
			 *         "key": "clientSecret",
			 *         "value": "z?{K9_.1%X1(n};bCX["
			 *       },
			 *       {
			 *         "key": "tenantId",
			 *         "value": "common"
			 *       }
			 *     ],
			 *     "provisioningState": "Succeeded",
			 *     "scopes": "User.Read, User.ReadBasic.All, Calendars.ReadWrite, People.Read",
			 *     "serviceProviderDisplayName": null,
			 *     "serviceProviderId": "30dd229c-58e3-4a48-bdfd-91ec48eb906c",
			 *     "settingId": "be361d58-0586-d620-a179-8bc6ace69787_24e99b70-0840-c957-537a"
			 *   },
			 *   "resourceGroup": "vasample",
			 *   "sku": null,
			 *   "tags": null,
			 *   "type": "Microsoft.BotService/botServices/connections"
			 * }
			 */
        } else {
            logger.error('Could not configure authentication connection automatically.');
            // $manualAuthRequired = $true
        }
    }
}
