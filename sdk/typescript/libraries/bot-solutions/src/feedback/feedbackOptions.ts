/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ActionTypes, CardAction } from 'botframework-schema';
import { ResponseManager } from '../responses/responseManager';
import { FeedbackResponses } from './feedbackResponses';

/**
 * Configures the FeedbackMiddleware object.
 */
export class FeedbackOptions {
    private _feedbackActions: CardAction[] | undefined;
    private _dismissAction: CardAction | undefined;
    private _feedbackReceivedMessage: string = '';
    private _commentPrompt: string = '';
    private _commentReceivedMessage: string = '';
    private readonly responseManager: ResponseManager;
    private _locale: string = 'en-us';

    /**
     * Gets or sets a value indicating whether gets or sets flag to prompt for free-form
     * comments for all or select feedback choices (comment prompt is shown after user selects a preset choice).
     * Default value is false.
     * A value indicating whether gets or sets flag to prompt for free-form comments for all or select feedback choices.
     */
    public commentsEnabled: boolean = false;

    public constructor() {
        this.responseManager = new ResponseManager(
            ['en', 'de', 'es', 'fr', 'it', 'zh'],
            [FeedbackResponses]
        );
    }

    public get locale(): string {
        return this._locale;
    }

    public set locale(locale: string){
        this._locale = locale;
    }

    /**
     * Gets custom feedback choices for the user.
     * Default values are "👍" and "👎".
     * @returns A `CardAction[]`.
     */
    public get feedbackActions(): CardAction[] {
        if (this._feedbackActions === undefined) {
            return [{ type: ActionTypes.PostBack,
                title: '👍',
                value: 'positive'
            },
            { type: ActionTypes.PostBack,
                title: '👎',
                value: 'negative'
            }];
        }

        return this._feedbackActions;
    }

    /**
     * Sets custom feedback choices for the user.
     * @param value Custom feedback choices for the user.
     */
    public set feedbackActions(value: CardAction[]) {
        this._feedbackActions = value;
    }

    /**
     * Gets text to show on button that allows user to hide/ignore the feedback request.
     * @returns A `CardAction`.
     */
    public get dismissAction(): CardAction {
        if (this._dismissAction === undefined) {
            return { type: ActionTypes.PostBack,
                title: this.responseManager.getResponseText(FeedbackResponses.dismissTitle, this._locale),
                value: 'dismiss'
            };
        }

        return this._dismissAction;
    }

    /**
     * Sets custom feedback choices for the user.
     * @param value Text to show on button that allows user to hide/ignore the feedback request.
     */
    public set dismissAction(value: CardAction) {
        this._dismissAction = value;
    }

    /**
     * Gets message to show when a user provides some feedback.
     * Default value is "Thanks for your feedback!".
     * @returns A feedbackReceivedMessage as `string`.
     */
    public get feedbackReceivedMessage(): string {
        if (this._feedbackReceivedMessage === undefined || this._feedbackReceivedMessage.trim().length === 0) {
            return this.responseManager.getResponseText(FeedbackResponses.feedbackReceivedMessage, this._locale);
        }

        return this._feedbackReceivedMessage;
    }

    /**
     * Sets message to show when a user provides some feedback.
     * @param value Message to show when a user provides some feedback.
     */
    public set feedbackReceivedMessage(value: string) {
        this._feedbackReceivedMessage = value;
    }

    /**
     * Gets the message to show when `CommentsEnabled` is `true`.
     * Default value is "Please add any additional comments in the chat.".
     * @returns A commentPrompt as `string`.
     */
    public get commentPrompt(): string {
        if (this._commentPrompt === undefined || this._commentPrompt.trim().length === 0) {
            return this.responseManager.getResponseText(FeedbackResponses.commentPrompt, this._locale);
        }

        return this._commentPrompt;
    }

    /**
     * Sets the message to show when `CommentsEnabled` is `true`.
     * @param value The message to show when `CommentsEnabled` is `true`.
     */
    public set commentPrompt(value: string) {
        this._commentPrompt = value;
    }

    /**
     * Gets the message to show when a user's comment has been received.
     * Default value is "Your comment has been received.".
     * @returns A commentReceivedMessage as `string`.
     */
    public get commentReceivedMessage(): string {
        if (this._commentReceivedMessage === undefined || this._commentReceivedMessage.trim().length === 0) {
            return this.responseManager.getResponseText(FeedbackResponses.commentReceivedMessage, this._locale);
        }

        return this._commentReceivedMessage;
    }

    /**
     * Sets the message to show when a user's comment has been received.
     * @param value The message to show when a user's comment has been received.
     */
    public set commentReceivedMessage(value: string) {
        this._commentReceivedMessage = value;
    }
}
