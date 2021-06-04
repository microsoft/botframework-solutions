/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    ActivityTypes,
    ChannelAccount,
    Channels,
    ConversationReference,
    IEndOfConversationActivity,
    IEventActivity,
    IMessageActivity } from 'botframework-schema';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace ActivityEx {
    export function createReply(source: Activity, text?: string, locale?: string): Activity {
        const reply: string = text || '';

        return {
            channelId: source.channelId,
            conversation: source.conversation,
            from: source.recipient,
            label: source.label,
            locale: locale,
            callerId: source.callerId,
            recipient: source.from,
            replyToId: source.id,
            serviceUrl: source.serviceUrl,
            text: reply,
            timestamp: new Date(),
            type: ActivityTypes.Message,
            valueType: source.valueType,
            localTimezone: source.localTimezone,
            listenFor: source.listenFor,
            semanticAction: source.semanticAction
        };
    }

    export function getContinuationActivity(source: Partial<ConversationReference>): Partial<Activity> {
        if (source === undefined) {
            throw new Error('source needs to be defined');
        }

        return {
            type: ActivityTypes.Event,
            name: 'ContinueConversation',
            channelId: source.channelId,
            serviceUrl: source.serviceUrl,
            conversation: source.conversation,
            recipient: source.bot,
            from: source.user,
            relatesTo: source as ConversationReference
        };
    }

    export function isStartActivity(activity: Activity): boolean {
        switch (activity.channelId) {
            case Channels.Skype: {
                if (activity.type === ActivityTypes.ContactRelationUpdate && activity.action === 'add') {
                    return true;
                }

                return false;
            }
            case Channels.Directline:
            case Channels.Emulator:
            case Channels.Webchat:
            case Channels.Msteams:
            case Channels.DirectlineSpeech:
            case Channels.Test: {
                if (activity.type === ActivityTypes.ConversationUpdate) {
                    // When bot is added to the conversation (triggers start only once per conversation)
                    if (activity.membersAdded !== undefined &&
                        activity.membersAdded.some((m: ChannelAccount): boolean => m.id === activity.recipient.id)) {
                        return true;
                    }
                }

                return false;
            }
            default:
                return false;
        }
    }

    export function createEventActivity(): Partial<IEventActivity> {
        return { type: ActivityTypes.Event };
    }

    export function createMessageActivity(): Partial<IMessageActivity> {
        return { type: ActivityTypes.Message };
    }

    export function createEndOfConversationActivity(): Partial<IEndOfConversationActivity> {
        return { type: ActivityTypes.EndOfConversation };
    }

    export function applyConversationReference(source: Partial<Activity>, reference: Partial<ConversationReference>, isComming?: boolean): Partial<Activity> {
        if (reference.channelId !== undefined) {
            source.channelId = reference.channelId;
        }

        if (reference.serviceUrl !== undefined) {
            source.serviceUrl = reference.serviceUrl;
        }
        
        if (reference.conversation !== undefined) {
            source.conversation = reference.conversation;
        }

        if(isComming) {
            if (reference.user !== undefined) {
                
            }

            if (reference.bot !== undefined) {
                source.recipient = reference.bot;
            }

            if(reference.activityId !== undefined) {
                source.id = reference.activityId;
            }
        } else {
            if(reference.bot !== undefined) {
                source.from = reference.bot;
            }
            if(reference.user !== undefined) {
                source.recipient = reference.user;
            }
            if(reference.activityId !== undefined) {
                source.replyToId = reference.activityId;
            }
        }

        return source;
    }
}
