package com.microsoft.bot.builder.solutions.virtualassistant;

interface ISpeechService {

    boolean isSpeechSdkRunning();
    void sendTextMessage(String msg);
    void initializeSpeechSdk(boolean haveRecordAudioPermission);
    void connectAsync();
    void startLocationUpdates();
    void resetBot();
    String getConfiguration();// the String is "Configuration" as JSON
    void setConfiguration(String json);// the String is "Configuration" as JSON
    void sendLocationEvent(String lat, String lon);
    void requestWelcomeCard();
    void injectReceivedActivity(String json);
    void listenOnceAsync();
    void sendActivityMessageAsync(String msg);
    String getSuggestedActions();//the String is "List<CardAction>" as JSON
    void clearSuggestedActions();
}
