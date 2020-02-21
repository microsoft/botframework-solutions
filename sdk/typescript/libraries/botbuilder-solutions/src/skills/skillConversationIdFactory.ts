/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConversationReference, SkillConversationIdFactoryBase, Storage } from 'botbuilder';

 /**
 * A SkillConversationIdFactory that uses IStorage to store and retrieve ConversationReference instances.
 */
export class SkillConversationIdFactory extends SkillConversationIdFactoryBase {

    public readonly storage: Storage;

    public constructor(storage: Storage) {
        super();
        
        if (storage === undefined) { throw new Error('The value of storage is undefined'); }
        this.storage = storage;
    }

    public async createSkillConversationId(conversationReference: ConversationReference): Promise<string> {

        if (conversationReference === null) { throw new Error('The value of conversationReference is undefined'); }

        if (conversationReference.conversation.id === undefined || conversationReference.conversation.id.trim().length <= 0) { 
            throw new Error('The value of conversationId is undefined'); 
        }
        
        if (conversationReference.channelId === undefined || conversationReference.channelId.trim().length <= 0) { 
            throw new Error('The value of channelId is undefined'); 
        }
        
        const storageKey: string = `${conversationReference.conversation.id}-${conversationReference.channelId}-skillconvo`;
        const skillConversationInfo: Map<string, ConversationReference> = new Map<string, ConversationReference>();
        skillConversationInfo.set(storageKey, conversationReference); 
        await this.storage.write(skillConversationInfo); 

        return storageKey;
    }

    public async getConversationReference(skillConversationId: string): Promise<ConversationReference>
    {
        if (skillConversationId === undefined || skillConversationId === "") { throw new Error('The value of skillConversationId is undefined or empty'); }

        const skillConversationInfo = await this.storage.read([skillConversationId]);
        if (skillConversationInfo)
        {
            const conversationInfo: ConversationReference = skillConversationInfo[skillConversationId];

            return conversationInfo;
        }

        throw new Error(`'conversationInfo' is undefined`);
    }

    public async deleteConversationReference(skillConversationId: string): Promise<void>
    {
        await this.storage.delete([skillConversationId]);
    }
}
