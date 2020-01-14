import { BotFrameworkAdapter, BotFrameworkAdapterSettings, BotTelemetryClient, InvokeResponse,
    Severity, TurnContext, WebRequest, WebResponse } from 'botbuilder';
import { Activity } from 'botframework-schema';
import { IActivityHandler } from '../activityHandler';
import { IAuthenticationProvider } from '../auth';
import { SkillHttpBotAdapter } from './skillHttpBotAdapter';
import { ClaimsIdentity } from 'botframework-connector';

/**
 * This adapter is responsible for accepting a bot-to-bot call over http transport.
 * It'll perform the following tasks:
 * 1. Authentication.
 * 2. Call SkillHttpBotAdapter to process the incoming activity.
 */
export class SkillHttpAdapter extends BotFrameworkAdapter {
    private readonly botAdapter: IActivityHandler;
    private readonly authenticationProvider?: IAuthenticationProvider;
    private readonly telemetryClient?: BotTelemetryClient;

    public constructor(
        botAdapter: SkillHttpBotAdapter,
        authenticationProvider?: IAuthenticationProvider,
        telemetryClient?: BotTelemetryClient,
        config?: Partial<BotFrameworkAdapterSettings>
    ) {
        super(config);
        this.botAdapter = botAdapter;
        this.authenticationProvider = authenticationProvider;
        this.telemetryClient = telemetryClient;
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    public async processActivity(req: WebRequest, res: WebResponse, logic: (context: TurnContext) => Promise<any>): Promise<void> {
        if (this.authenticationProvider) {
            // grab the auth header from the inbound http request
            const authHeader: string = req.headers.authorization || req.headers.Authorization || '';
            const authenticated: ClaimsIdentity = await this.authenticationProvider.authenticate(authHeader);

            if (!authenticated) {
                res.status(401);
                res.end();

                return;
            }
        }

        // deserialize the incoming Activity
        const activity: Activity = await parseRequest(req);

        if (this.telemetryClient) {
            const message: string = `SkillHttpAdapter: Processing incoming activity. Activity id: ${activity.id}`;
            this.telemetryClient.trackTrace({
                message: message,
                severityLevel: Severity.Information
            });
        }

        // process the inbound activity with the bot
        const invokeResponse: InvokeResponse = await this.botAdapter.processActivity(activity, logic);

        // write the response, potentially serializing the InvokeResponse
        res.status(invokeResponse.status);
        if (invokeResponse.body) {
            res.send(invokeResponse.body);
        }

        res.end();
    }
}

async function parseRequest(req: WebRequest): Promise<Activity> {
    return new Promise((resolve, reject): void => {
        function returnActivity(activity: Activity): void {
            if (typeof activity !== 'object') { throw new Error(`BotFrameworkAdapter.parseRequest(): invalid request body.`); }
            if (typeof activity.type !== 'string') { throw new Error(`BotFrameworkAdapter.parseRequest(): missing activity type.`); }
            if (typeof activity.timestamp === 'string') { activity.timestamp = new Date(activity.timestamp); }
            if (typeof activity.localTimestamp === 'string') { activity.localTimestamp = new Date(activity.localTimestamp); }
            if (typeof activity.expiration === 'string') { activity.expiration = new Date(activity.expiration); }
            resolve(activity);
        }

        if (req.body) {
            try {
                returnActivity(req.body);
            } catch (err) {
                reject(err);
            }
        } else {
            let requestData: string = '';
            req.on('data', (chunk: string): void => {
                requestData += chunk;
            });
            req.on('end', (): void => {
                try {
                    req.body = JSON.parse(requestData);
                    returnActivity(req.body);
                } catch (err) {
                    reject(err);
                }
            });
        }
    });
}
