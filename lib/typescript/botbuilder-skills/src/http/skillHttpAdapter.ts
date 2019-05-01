import { BotFrameworkAdapter } from 'botbuilder';

/**
 * This adapter is responsible for accepting a bot-to-bot call over http transport.
 * It'll perform the following tasks:
 * 1. Authentication.
 * 2. Call SkillHttpBotAdapter to process the incoming activity.
 */
export class SkillHttpAdapter extends BotFrameworkAdapter {
    // PENDING botbuilder-js do not has IBotFrameworkHttpAdapter
}
