package com.microsoft.bot.builder.solutions.virtualassistant;

import android.app.Application;
import android.content.Intent;
import android.support.v4.content.ContextCompat;

import com.microsoft.bot.builder.solutions.virtualassistant.service.SpeechService;

public class MainApplication extends Application {

    // STATE
    private static MainApplication instance;

    @Override
    public void onCreate() {
        super.onCreate();
        instance = this;

        // start service but don't initialize it yet
        Intent intent = new Intent(this, SpeechService.class);
        intent.setAction(SpeechService.ACTION_START_FOREGROUND_SERVICE);
        ContextCompat.startForegroundService(this, intent);
    }

    public static MainApplication getInstance(){
        return instance;
    }
}
