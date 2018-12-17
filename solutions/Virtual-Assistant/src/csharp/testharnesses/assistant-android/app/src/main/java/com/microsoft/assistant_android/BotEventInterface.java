package com.microsoft.assistant_android;

public interface BotEventInterface {
    void onMessageReceived(String message);
    void onBotReady();
    void onBotError(String message);
}
