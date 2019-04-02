// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    Activity,
    ActivityTypes,
    ChannelAccount,
    ConversationReference } from 'botbuilder';

export namespace ActivityExtensions {
    export function createReply(source: Activity, text?: string, local?: string): Activity {
        const reply: string = text || '';

        return {
            channelId: source.channelId,
            conversation: source.conversation,
            from: source.recipient,
            label: source.label,
            locale: local,
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
        if (!source) {
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
            relatesTo: <ConversationReference> source
        };
    }

    export function isStartActivity(activity: Activity): boolean {
        switch (activity.channelId) {
            case 'skype': {
                if (activity.type === ActivityTypes.ContactRelationUpdate && activity.action === 'add') {
                    return true;
                }

                return false;
            }
            case 'directline':
            case 'emulator':
            case 'webchat':
            case 'msteams': {
                if (activity.type === ActivityTypes.ConversationUpdate) {
                    if (activity.membersAdded && activity.membersAdded.some((m: ChannelAccount) => m.id === activity.recipient.id)) {
                        // When bot is added to the conversation (triggers start only once per conversation)
                        return true;
                    }
                }

                return false;
            }
            default:
                return false;
        }
    }
}
