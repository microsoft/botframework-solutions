import { TurnContext } from 'botbuilder';
import { MicrosoftAppCredentials } from 'botframework-connector';
import { Activity } from 'botframework-schema';
import { ISkillTransport, TokenRequestHandler } from '../skillTransport';
import { ISkillManifest } from '../models';

export class SkillWebSocketTransport implements ISkillTransport {
    constructor(
        skillManifest: ISkillManifest,
        appCredentials: MicrosoftAppCredentials
    ) {
        
    }

    public forwardToSkill(
        turnContext: TurnContext,
        activity: Partial<Activity>,
        tokenRequestHandler?: TokenRequestHandler|undefined
    ): Promise<boolean> {
        throw new Error("Method not implemented.");
    }

    public cancelRemoteDialogs(turnContext: TurnContext): Promise<void> {
        throw new Error("Method not implemented.");
    }

    public disconnect(): void {
        throw new Error("Method not implemented.");
    }
}
