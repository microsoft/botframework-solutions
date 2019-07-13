import { BotTelemetryClient, TurnContext } from 'botbuilder';
import { ActivityExtensions } from 'botbuilder-solutions';
import { MicrosoftAppCredentials } from 'botframework-connector';
import { Activity, ActivityTypes } from 'botframework-schema';
import { CancellationToken, IStreamingTransportClient, Request, RequestHandler } from 'microsoft-bot-protocol';
import { Client } from 'microsoft-bot-protocol-websocket';
import { ISkillManifest, SkillEvents } from '../models';
import { SkillCallingRequestHandler } from '../skillCallingRequestHandler';
import { ISkillTransport, TokenRequestHandler } from '../skillTransport';

export class SkillWebSocketTransport implements ISkillTransport {
    private readonly cToken: CancellationToken;
    private readonly skillManifest: ISkillManifest;
    private readonly appCredentials: MicrosoftAppCredentials;
    private readonly telemetryClient: BotTelemetryClient;
    private streamingTransportClient!: IStreamingTransportClient;
    private endOfConversation: boolean;

    public constructor(
        skillManifest: ISkillManifest,
        appCredentials: MicrosoftAppCredentials,
        telemetryClient: BotTelemetryClient,
        streamingTransportClient?: IStreamingTransportClient
    ) {
        this.skillManifest = skillManifest;
        this.appCredentials = appCredentials;
        this.telemetryClient = telemetryClient;

        if (streamingTransportClient) {
            this.streamingTransportClient = streamingTransportClient;
        }

        this.cToken = new CancellationToken();
        this.endOfConversation = false;
    }

    public async forwardToSkill(
        turnContext: TurnContext,
        activity: Partial<Activity>,
        tokenRequestHandler?: TokenRequestHandler|undefined
    ): Promise<boolean> {
        if (this.streamingTransportClient === undefined) {
            // acquire AAD token
            MicrosoftAppCredentials.trustServiceUrl(this.skillManifest.endpoint);
            // put AAD token in the header
            // MISSING Client doesn't has ctor with headers

            // establish websocket connection

            const url: string = ensureWebSocketUrl(this.skillManifest.endpoint);
            const requestHandler: RequestHandler = new SkillCallingRequestHandler(
                turnContext,
                this.telemetryClient,
                tokenRequestHandler,
                this.handoffActivity
            );
            this.streamingTransportClient = new Client({url: url, requestHandler: requestHandler});

            await this.streamingTransportClient.connectAsync();
        }

        // set recipient to the skill
        if (activity !== undefined && activity.recipient !== undefined) {
            const recipientId: string = activity.recipient.id;
            activity.recipient.id = this.skillManifest.msAppId;

            // Serialize the activity and POST to the Skill endpoint
            const request: Request = Request.create('POST', '');
            request.setBody(JSON.stringify(activity));

            // set back recipient id to make things consistent
            activity.recipient.id = recipientId;
            await this.streamingTransportClient.sendAsync(request, this.cToken);
        }

        return this.endOfConversation;
    }

    public async cancelRemoteDialogs(turnContext: TurnContext): Promise<void> {
        const cancelRemoteDialogEvent: Activity = ActivityExtensions.createReply(turnContext.activity);
        cancelRemoteDialogEvent.type = ActivityTypes.Event;
        cancelRemoteDialogEvent.name = SkillEvents.cancelAllSkillDialogsEventName;

        await this.forwardToSkill(turnContext, cancelRemoteDialogEvent);
    }

    public disconnect(): void {
        if (this.streamingTransportClient !== undefined) {
            this.streamingTransportClient.disconnect();
        }
    }

    private async handoffActivity(activity: Activity): Promise<void> {
        this.endOfConversation = true;
    }
}

function ensureWebSocketUrl(url: string): string {
    if (!url) {
        throw new Error('url is empty!');
    }

    // tslint:disable-next-line:no-http-string
    const httpPrefix: string = 'http://';
    const httpsPrefix: string = 'https://';
    const wsPrefix: string = 'ws://';
    const wssPrefix: string = 'wss://';

    if (url.startsWith(httpPrefix)) {
        return url.replace(httpPrefix, wsPrefix);
    }

    if (url.startsWith(httpsPrefix)) {
        return url.replace(httpsPrefix, wssPrefix);
    }

    return url;
}
