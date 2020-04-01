/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConversationReference, SkillConversationIdFactoryBase, Storage, SkillConversationIdFactoryOptions, SkillConversationReference, StoreItems, TurnContext } from 'botbuilder';

/**
 * A SkillConversationIdFactory that uses IStorage to store and retrieve ConversationReference instances.
 */
export class SkillConversationIdFactory extends SkillConversationIdFactoryBase {
    private readonly storage: Storage;

    public constructor(storage: Storage) {
        super();
        
        if (storage === undefined) { throw new Error('The value of storage is undefined'); }
        this.storage = storage;
    }

    public async createSkillConversationIdWithOptions(options: SkillConversationIdFactoryOptions): Promise<string> {
        if (options === undefined) { throw new Error('The value of options is undefined')};

        // Create the storage key based on the SkillConversationIdFactoryOptions
        const conversationReference: Partial<ConversationReference> = TurnContext.getConversationReference(options.activity);
        if (conversationReference === undefined) { throw new Error('The value of conversationReference is undefined'); }
        if (conversationReference.conversation === undefined) { throw new Error('The value of conversationReference.conversation is undefined'); }
        const storageKey: string = `${conversationReference.conversation.id}-${options.botFrameworkSkill.id}-${conversationReference.channelId}-skillconvo`;

        // Create the SkillConversationReference
        const skillConversationReference: SkillConversationReference = {
            conversationReference: conversationReference as ConversationReference,
            oAuthScope: options.fromBotOAuthScope
        }

        // Store the SkillConversationReference
        const skillConversationInfo: StoreItems = {} as StoreItems;
        skillConversationInfo[storageKey] = skillConversationReference;
        await this.storage.write(skillConversationInfo); 

        // Return the storageKey (that will be also used as the conversation ID to call the skill)
        return storageKey;
    }

    public async getSkillConversationReference(skillConversationId: string): Promise<SkillConversationReference>
    {
        if (skillConversationId === undefined || skillConversationId.trim().length === 0) { throw new Error('The value of skillConversationId is undefined or empty'); }

        // Get the SkillConversationReference from storage for the given skillConversationId.
        const skillConversationInfo: StoreItems = await this.storage.read([skillConversationId]);
        if (Object.keys(skillConversationInfo).length > 0) {
            const conversationInfo: SkillConversationReference = skillConversationInfo[skillConversationId];

            return conversationInfo;
        }

        throw new Error(`'skillConversationInfo' is undefined`);
    }

    public async deleteConversationReference(skillConversationId: string): Promise<void>
    {
        // Delete the SkillConversationReference from storage
        await this.storage.delete([skillConversationId]);
    }
}
