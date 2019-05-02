import { TurnContext } from 'botbuilder';
import { MicrosoftAppCredentials } from 'botframework-connector';
import { Activity } from 'botframework-schema';
import { ISkillManifest } from '../models';
import { ISkillTransport, TokenRequestHandler } from '../skillTransport';

export class SkillWebSocketTransport implements ISkillTransport {
    private readonly skillManifest: ISkillManifest;
    private readonly appCredentials: MicrosoftAppCredentials;

    constructor(
        skillManifest: ISkillManifest,
        appCredentials: MicrosoftAppCredentials
    ) {
        this.skillManifest = skillManifest;
        this.appCredentials = appCredentials;
    }

    public forwardToSkill(
        turnContext: TurnContext,
        activity: Partial<Activity>,
        tokenRequestHandler?: TokenRequestHandler|undefined
    ): Promise<boolean> {
        throw new Error('Method not implemented.');
    }

    public cancelRemoteDialogs(turnContext: TurnContext): Promise<void> {
        throw new Error('Method not implemented.');
    }

    public disconnect(): void {
        throw new Error('Method not implemented.');
    }
}
