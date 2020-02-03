/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import {
    Activity,
    BotTelemetryClient,
    CardFactory,
    ConversationState,
    MessageFactory,
    Middleware,
    StatePropertyAccessor,
    TurnContext } from 'botbuilder';
// tslint:disable-next-line: no-submodule-imports //supportsSuggestedActions not exported, botbuilder-js#1354
import { supportsSuggestedActions } from 'botbuilder-dialogs/lib/choices/channel';
import { Attachment, CardAction } from 'botframework-schema';
import { FeedbackOptions } from './feedbackOptions';
import { FeedbackRecord } from './feedbackRecord';

export class FeedbackMiddleware implements Middleware {
    private static options: FeedbackOptions;
    private static feedbackAccessor: StatePropertyAccessor<FeedbackRecord>;
    private readonly conversationState: ConversationState;
    private readonly telemetryClient: BotTelemetryClient;
    private readonly traceName: string = 'Feedback';

    /**
     * Initializes a new instance of the FeedbackMiddleware class
     * @param conversationState The conversation state used for storing the feedback record before logging to Application Insights.
     * @param telemetryClient The bot telemetry client used for logging the feedback record in Application Insights.
     * @param options (Optional ) Feedback options object configuring the feedback actions and responses.
     */
    public constructor(
        conversationState: ConversationState,
        telemetryClient: BotTelemetryClient,
        options?: FeedbackOptions) {
        if (conversationState === undefined) { throw new Error('The value of conversationState is undefined'); }
        if (telemetryClient === undefined) { throw new Error('The value of telemetryClient is undefined'); }
        this.conversationState = conversationState;
        this.telemetryClient = telemetryClient;
        FeedbackMiddleware.options = options !== undefined ? options : new FeedbackOptions();

        // Create FeedbackRecord state accessor
        FeedbackMiddleware.feedbackAccessor = conversationState.createProperty<FeedbackRecord>(FeedbackRecord.name);
    }

    /**
     * Sends a Feedback Request activity with suggested actions to the user.
     * @param context Turn context for sending activities.
     * @param tag Tag to categorize feedback record in Application Insights.
     * @returns A Task representing the asynchronous operation.
     */
    public static async requestFeedback(context: TurnContext, tag: string): Promise<void> {
        // clear state
        await FeedbackMiddleware.feedbackAccessor.delete(context);

        // create feedbackRecord with original activity and tag
        const record: FeedbackRecord = {
            request: context.activity,
            tag: tag
        };

        // store in state. No need to save changes, because its handled in IBot
        await this.feedbackAccessor.set(context, record);

        // If channel supports suggested actions
        if (supportsSuggestedActions(context.activity.channelId)) {
            // prompt for feedback
            // if activity already had suggested actions, add the feedback actions
            if (context.activity.suggestedActions !== undefined) {
                const actions: CardAction[] = [
                    ...context.activity.suggestedActions.actions,
                    ...this.getFeedbackActions()
                ];

                await context.sendActivity(MessageFactory.suggestedActions(actions));
            } else {
                const actions: CardAction[] = this.getFeedbackActions();
                await context.sendActivity(MessageFactory.suggestedActions(actions));
            }
        } else {
            // else channel doesn't support suggested actions, so use hero card.
            const hero: Attachment = CardFactory.heroCard('', undefined, this.getFeedbackActions());
            await context.sendActivity(MessageFactory.attachment(hero));
        }
    }

    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        // get feedback record from state. If we don't find anything, set to null.
        const record: FeedbackRecord = await FeedbackMiddleware.feedbackAccessor.get(context, new FeedbackRecord());

        // if we have requested feedback
        if (record !== undefined) {
            // if activity text matches a feedback action
            // save feedback in state
            const feedback: CardAction | undefined = FeedbackMiddleware.options.feedbackActions.find((cardAction: CardAction): boolean => {
                return (context.activity.text === cardAction.value as string || context.activity.text === cardAction.title);
            });

            if (feedback !== undefined) {
                // Set the feedback to the action value for consistency
                record.feedback = feedback.value as string;
                await FeedbackMiddleware.feedbackAccessor.set(context, record);

                if (FeedbackMiddleware.options.commentsEnabled) {
                    // if comments are enabled
                    // create comment prompt with dismiss action
                    if (supportsSuggestedActions(context.activity.channelId)) {
                        const commentPrompt: Partial<Activity> = MessageFactory.suggestedActions(
                            [FeedbackMiddleware.options.dismissAction],
                            `${ FeedbackMiddleware.options.feedbackReceivedMessage } ${ FeedbackMiddleware.options.commentPrompt }`
                        );

                        // prompt for comment
                        await context.sendActivity(commentPrompt);
                    } else {
                        // channel doesn't support suggestedActions, so use hero card.
                        const hero: Attachment = CardFactory.heroCard(FeedbackMiddleware.options.commentPrompt, undefined, [FeedbackMiddleware.options.dismissAction]);

                        // prompt for comment
                        await context.sendActivity(MessageFactory.attachment(hero));
                    }
                } else {
                    // comments not enabled, respond and cleanup
                    // send feedback response
                    await context.sendActivity(FeedbackMiddleware.options.feedbackReceivedMessage);

                    // log feedback in appInsights
                    this.logFeedback(record);

                    // clear state
                    await FeedbackMiddleware.feedbackAccessor.delete(context);
                }
            } else if (context.activity.text !== undefined
                    && (context.activity.text === FeedbackMiddleware.options.dismissAction.value as string
                    || context.activity.text === FeedbackMiddleware.options.dismissAction.title)) {
                // if user dismissed
                // log existing feedback
                if (record.feedback !== undefined && record.feedback.trim().length > 0) {
                    // log feedback in appInsights
                    this.logFeedback(record);
                }

                // clear state
                await FeedbackMiddleware.feedbackAccessor.delete(context);
            } else if (record.feedback !== undefined && record.feedback.trim().length > 0 && FeedbackMiddleware.options.commentsEnabled) {
                // if we received a comment and user didn't dismiss
                // store comment in state
                record.comment = context.activity.text;
                await FeedbackMiddleware.feedbackAccessor.set(context, record);

                // Respond to comment
                await context.sendActivity(FeedbackMiddleware.options.commentReceivedMessage);

                // log feedback in appInsights
                this.logFeedback(record);

                // clear state
                await FeedbackMiddleware.feedbackAccessor.delete(context);
            } else {
                // we requested feedback, but the user responded with something else
                // clear state and continue (so message can be handled by dialog stack)
                await FeedbackMiddleware.feedbackAccessor.delete(context);
                await next();
            }

            await this.conversationState.saveChanges(context);
        } else {
            // We are not requesting feedback. Go to next.
            await next();
        }
    }

    private static getFeedbackActions(): CardAction[] {
        return [
            ...FeedbackMiddleware.options.feedbackActions,
            FeedbackMiddleware.options.dismissAction
        ];
    }

    private logFeedback(record: FeedbackRecord): void {
        const properties: { [id: string]: string } = {
            tag: record.tag !== undefined ? record.tag : '',
            feedback: record.feedback !== undefined ? record.feedback : '',
            comment: record.comment !== undefined ? record.comment : '',
            text: record.request !== undefined ? record.request.text : '',
            id: record.request !== undefined ? record.request.id !== undefined ? record.request.id : '' : '',
            channelId: record.request !== undefined ? record.request.channelId : ''
        };
        this.telemetryClient.trackEvent({
            name: this.traceName,
            properties: [
                properties
            ]
        });
    }
}
