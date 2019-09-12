package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import client.model.BotConnectorActivity;

public class ChatModel {
    public BotConnectorActivity botConnectorActivity;
    public String userRequest;

    public ChatModel(BotConnectorActivity botConnectorActivity) {
        this.botConnectorActivity = botConnectorActivity;
    }

    public ChatModel(String userRequest) {
        this.userRequest = userRequest;
    }

    public boolean isBotMessage() {
        return this.userRequest == null;
    }

    public boolean hasAttachments() {
        if (this.botConnectorActivity != null) {
            if (this.botConnectorActivity.getAttachments() != null) {
                return this.botConnectorActivity.getAttachments().size() > 0;
            }
        }
        return false;
    }
}
