import { CosmosDbStorageSettings } from 'botbuilder-azure';
import { LocaleConfiguration } from './localeConfiguration';

export abstract class SkillConfigurationBase {

    public isAuthenticatedSkill: boolean = false;

    public abstract authenticationConnections: { [key: string]: string };

    public abstract cosmosDbOptions: CosmosDbStorageSettings;

    public abstract localeConfigurations: Map<string, LocaleConfiguration>;

    public abstract properties: { [key: string]: Object|undefined };
}
