/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ContentModeratorClient } from 'azure-cognitiveservices-contentmoderator';
import { Middleware, TurnContext  } from 'botbuilder';
import { ActivityTypes } from 'botframework-schema';
import { CognitiveServicesCredentials } from 'ms-rest-azure';
import { Readable } from 'stream';

export class ContentModeratorMiddleware implements Middleware {
    /**
     * Key for Text Moderator result in Bot Context dictionary.
     */
    public readonly serviceName: string = 'ContentModerator';
    /**
     * Key for Text Moderator result in Bot Context dictionary.
     */
    public readonly textModeratorResultKey: string = 'TextModeratorResult';
    /**
     * Content Moderator service key.
     */
    public readonly subscriptionKey: string;
    /**
     * Content Moderator service region.
     */
    public readonly region: string;

    /**
     * Initializes a new instance of the "ContentModeratorMiddleware" class.
     * @param subscriptionKey Azure Service Key.
     * @param region Azure Service Region.
     */
    constructor(subscriptionKey: string, region: string) {
        this.subscriptionKey = subscriptionKey;
        this.region = region;
    }

    /**
     * Analyzes activity text with Content Moderator and adds result to Bot Context. Run on each turn of the conversation.
     * @param context The Bot Context object.
     * @param next The next middleware component to run.
     */
    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        if (context === undefined) {
            throw new Error('Context not found.');
        }

        if (context.activity.type === ActivityTypes.Message) {
            const textStream: Readable = this.textToReadable(context.activity.text);

            const credentials: CognitiveServicesCredentials = new CognitiveServicesCredentials(this.subscriptionKey);
            const client: ContentModeratorClient = new ContentModeratorClient(credentials, `${this.region}.api.cognitive.microsoft.com`);

            const screenResult: Object = await client.textModeration.screenText(
                'text/plain',
                textStream,
                {
                    language: 'en',
                    autocorrect: true,
                    pII: true,
                    listId: undefined,
                    classify: true
                }
            );

            context.turnState.set(this.textModeratorResultKey, screenResult);
        }

        await next();
    }

    private textToReadable(text: string): Readable {
        const readable: Readable = new Readable();
        // tslint:disable-next-line:no-empty
        readable._read = (): void => { };
        readable.push(text);
        readable.push(undefined);

        return readable;
    }
}
