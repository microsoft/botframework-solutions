package com.microsoft.bot.builder.solutions.directlinespeech;

import android.annotation.SuppressLint;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.res.AssetManager;
import android.util.Log;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;

import java.io.IOException;

public class ConfigurationManager {

    // CONSTANTS
    final private static String SHARED_PREFS_NAME = "va_shared_prefs";
    final private static String SHARED_PREFS_CONFIGURATION = "va_configuration";
    final private static String LOGTAG = "ConfigurationManager";

    // STATE
    private SharedPreferences sharedPreferences;
    private Gson gson;

    public ConfigurationManager(Context context) {
        sharedPreferences = context.getSharedPreferences(SHARED_PREFS_NAME, Context.MODE_PRIVATE);
        gson = new Gson();
    }

    @SuppressLint("ApplySharedPref")
    public void clearConfiguration(){
        SharedPreferences.Editor editor = sharedPreferences.edit();
        editor.clear().commit();
    }

    public void setConfiguration(Configuration configuration){
        SharedPreferences.Editor editor = sharedPreferences.edit();
        String json = gson.toJson(configuration);
        editor.putString(SHARED_PREFS_CONFIGURATION, json);
        editor.apply();
    }

    public Configuration getConfiguration(){
        String json = sharedPreferences.getString(SHARED_PREFS_CONFIGURATION, "{}");
        return gson.fromJson(json, new TypeToken<Configuration>(){}.getType());
    }


}
