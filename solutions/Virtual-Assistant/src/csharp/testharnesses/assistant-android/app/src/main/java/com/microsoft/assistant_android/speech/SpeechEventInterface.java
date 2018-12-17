package com.microsoft.assistant_android.speech;

public interface SpeechEventInterface {
    void onSpeechRecognized(String text);
    void onSpeechRecognizing(String text);
    void onDoneSpeaking();
    void onInitialized();
    void onSpeechError(String utterandeId);
    void onSpeechRecoError(String message);
}
