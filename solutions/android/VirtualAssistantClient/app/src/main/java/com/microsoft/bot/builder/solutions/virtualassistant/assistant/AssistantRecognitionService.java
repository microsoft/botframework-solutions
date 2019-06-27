package com.microsoft.bot.builder.solutions.virtualassistant.assistant;

import android.content.Intent;
import android.speech.RecognitionService;
import android.util.Log;

/**
 * Stub recognition service needed to be a complete voice interactor.
 */
public class AssistantRecognitionService extends RecognitionService {

    private static final String LOGTAG = "AssistantRecognitionSvc";

    @Override
    public void onCreate() {
        super.onCreate();
        Log.i(LOGTAG, "onCreate");
    }

    @Override
    protected void onStartListening(Intent recognizerIntent, Callback listener) {
        Log.i(LOGTAG, "onStartListening");
    }

    @Override
    protected void onCancel(Callback listener) {
        Log.i(LOGTAG, "onCancel");
    }

    @Override
    protected void onStopListening(Callback listener) {
        Log.i(LOGTAG, "onStopListening");
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        Log.i(LOGTAG, "onDestroy");
    }

}
