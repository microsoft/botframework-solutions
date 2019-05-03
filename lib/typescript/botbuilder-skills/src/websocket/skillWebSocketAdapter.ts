import { BotFrameworkAdapter, BotFrameworkAdapterSettings, BotTelemetryClient, NullTelemetryClient, TurnContext, WebRequest, WebResponse } from 'botbuilder';
import { Server, Socket } from 'microsoft-bot-protocol-websocket';
import { SkillWebSocketBotAdapter } from './skillWebSocketBotAdapter';
import { BotCallbackHandler } from '../activityHandler';
import { IAuthenticationProvider } from '../auth';
import { SkillWebSocketRequestHandler } from './skillWebSocketRequestHandler';

/**
 * This adapter is responsible for accepting a bot-to-bot call over websocket transport.
 * It'll perform the following tasks:
 * 1. Authentication.
 * 2. Create RequestHandler to handle follow-up websocket frames.
 * 3. Start listening on the websocket connection.
 */
export class SkillWebSocketAdapter extends BotFrameworkAdapter {
    private readonly authenticationProvider?: IAuthenticationProvider;
    private readonly telemetryClient: BotTelemetryClient;
    private readonly botAdapter: SkillWebSocketBotAdapter;

    constructor(
        botAdapter: SkillWebSocketBotAdapter,
        authenticationProvider?: IAuthenticationProvider,
        telemetryClient?: BotTelemetryClient,
        config?: Partial<BotFrameworkAdapterSettings>
    ) {
        super(config);
        this.botAdapter = botAdapter;
        this.authenticationProvider = authenticationProvider;
        this.telemetryClient = telemetryClient || new NullTelemetryClient();
    }

    // tslint:disable-next-line:no-any
    public async processActivity(req: WebRequest, res: WebResponse, logic: (context: TurnContext) => Promise<any>): Promise<void> {
        await this.createWebSocketConnection(req, logic);
    }

    private async createWebSocketConnection(req: any, bot: BotCallbackHandler) {
        // MISSING found an equivalent to websocket in TypeScript
        const socket: Socket = req.socket;
        const handler: SkillWebSocketRequestHandler = new SkillWebSocketRequestHandler(this.telemetryClient);
        const server = new Server(socket, handler);
        // MISSING the Server class does not exposes Disconnected handler
        // in C# server.Disconnected += Server_Disconnected;
        this.botAdapter.server = server;
        handler.bot = bot;
        handler.activityHandler = this.botAdapter;

        const startListening: string = await server.startAsync();
        console.log(startListening);
    }
}
