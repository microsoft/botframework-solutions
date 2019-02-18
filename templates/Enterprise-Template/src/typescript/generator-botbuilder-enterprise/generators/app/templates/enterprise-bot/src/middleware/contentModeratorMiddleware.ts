// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ContentModeratorClient, ContentModeratorModels } from 'azure-cognitiveservices-contentmoderator';
import { ActivityTypes, Middleware, TurnContext } from 'botbuilder';
import { CognitiveServicesCredentials } from 'ms-rest-azure';
import { Readable } from 'stream';

/**
 * Middleware component to run Content Moderator Service on all incoming activities.
 */
export class ContentModeratorMiddleware implements Middleware {
    /**
     * Key for Text Moderator result in Bot Context dictionary.
     */
    public static readonly serviceName: string = 'ContentModerator';

    /**
     * Key for Text Moderator result in Bot Context dictionary.
     */
    public static readonly textModeratorResultKey: string = 'TextModeratorResult';
    /**
     * Content Moderator service key.
     */
    public static readonly subscriptionKey: string;
    /**
     * Content Moderator service region.
     */
    private static readonly region: string;
    /**
     * Key for Text Moderator result in Bot Context dictionary.
     */
    private readonly contentModeratorClient: ContentModeratorClient;

    /**
     * Initializes a new instance of the ContentModeratorMiddleware class.
     * @param subscriptionKey Azure Service Key.
     * @param region Azure Service Region.
     */
    constructor(subscriptionKey: string, region: string) {
        this.contentModeratorClient = new ContentModeratorClient(
            new CognitiveServicesCredentials(subscriptionKey),
            `https://${region}.api.cognitive.microsoft.com`
            );
    }

    /**
     * Analyzes activity text with Content Moderator and adds result to Bot Context. Run on each turn of the conversation.
     * @param context - The Bot Context object.
     * @param next - The next middleware component to run.
     * @returns A Promise representing the asynchronous operation.
     */
    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        if (context.activity.type === ActivityTypes.Message) {

            const content: Readable = new Readable();
            content.push(context.activity.text);
            content.push(undefined);
            const screenResult: ContentModeratorModels.Screen = await this.contentModeratorClient.textModeration.screenText(
                'text/plain',
                content, {
                    language: 'eng',
                    autocorrect: true,
                    pII: true,
                    classify: true
                });
            context.turnState.set(ContentModeratorMiddleware.textModeratorResultKey, screenResult);
        }

        await next();
    }
}
