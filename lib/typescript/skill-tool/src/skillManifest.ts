export interface ISkillManifest {
        id: string;
        name: string;
        endpoint: string;
        description: string;
        suggestedAction: string;
        iconUrl: string;
        authenticationConnections: IAuthenticationconnection[];
        actions: IAction[];
    }

export interface IAuthenticationconnection {
        id: string;
        serviceProviderId: string;
        scopes: string;
    }

export interface IAction {
        id: string;
        definition: IDefinition;
    }

export interface IDefinition {
        description: string;
        slots: ISlot[];
        triggers: ITriggers;
    }

export interface ITriggers {
        utterances: IUtterance[];
        events: IEvent[];
    }

export interface IUtterance {
        locale: string;
        source: string[];
    }

export interface IEvent {
        name: string;
    }

export interface ISlot {
        name: string;
        types: string[];
}
