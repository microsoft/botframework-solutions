package com.microsoft.bot.builder.solutions.virtualassistant;

import android.app.Application;
import android.content.Intent;
import android.content.SharedPreferences;
import android.support.v4.content.ContextCompat;
import android.support.v7.app.AppCompatDelegate;

import com.microsoft.bot.builder.solutions.virtualassistant.service.SpeechService;

import static com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity.SHARED_PREFS_NAME;
import static com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity.SHARED_PREF_DARK_MODE;

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

        // read the dark-mode setting (necessary because setDefaultNightMode() doesn't persist between app restarts)
        SharedPreferences sharedPreferences = getSharedPreferences(SHARED_PREFS_NAME, MODE_PRIVATE);
        boolean darkModeEnabled = sharedPreferences.getBoolean(SHARED_PREF_DARK_MODE, false);
        AppCompatDelegate.setDefaultNightMode(darkModeEnabled?AppCompatDelegate.MODE_NIGHT_YES:AppCompatDelegate.MODE_NIGHT_NO);
    }

    public static MainApplication getInstance(){
        return instance;
    }
}
