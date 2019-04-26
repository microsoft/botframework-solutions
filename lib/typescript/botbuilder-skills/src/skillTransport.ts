import { TurnContext } from 'botbuilder';
import { Activity } from 'botframework-schema';

export type TokenRequestHandler = (activity: Activity) => Promise<Activity|undefined>;

export interface ISkillTransport {
    forwardToSkill(turnContext: TurnContext, activity: Partial<Activity>, tokenRequestHandler?: TokenRequestHandler): Promise<boolean>;
    cancelRemoteDialogs(turnContext: TurnContext): Promise<void>;
    disconnect(): void;
}
