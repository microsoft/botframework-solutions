package com.microsoft.bot.builder.solutions.directlinespeech;

import android.content.Context;
import android.content.SharedPreferences;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;

import java.io.IOException;
import java.io.InputStream;

public class ConfigurationManager {

    // CONSTANTS
    final private static String SHARED_PREFS_NAME = "va_shared_prefs";
    final private static String SHARED_PREFS_CONFIGURATION = "va_configuration";
    final private static String DEFAULT_CONFIGURATION_FILE = "default_configuration.json";
    final private static String LOGTAG = "ConfigurationManager";

    // STATE
    private SharedPreferences sharedPreferences;
    private Gson gson;
    private Configuration defaultConfiguration;

    public ConfigurationManager(Context context) {
        sharedPreferences = context.getSharedPreferences(SHARED_PREFS_NAME, Context.MODE_PRIVATE);
        gson = new Gson();
        try {
            InputStream is = context.getAssets().open(DEFAULT_CONFIGURATION_FILE);
            int size = is.available();
            byte[] buffer = new byte[size];
            is.read(buffer);
            is.close();
            String jsonString = new String(buffer, "UTF-8");
            defaultConfiguration = gson.fromJson(jsonString, new TypeToken<Configuration>(){}.getType());
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public void setConfiguration(Configuration configuration){
        SharedPreferences.Editor editor = sharedPreferences.edit();
        String json = gson.toJson(configuration);
        editor.putString(SHARED_PREFS_CONFIGURATION, json);
        editor.apply();
    }

    public Configuration getConfiguration(){
        String json = sharedPreferences.getString(SHARED_PREFS_CONFIGURATION, "{}");
        Configuration configuration = gson.fromJson(json, new TypeToken<Configuration>(){}.getType());
        if (configuration.serviceKey == null) {
            configuration.serviceKey = defaultConfiguration.serviceKey;
        }
        if (configuration.serviceRegion == null) {
            configuration.serviceRegion = defaultConfiguration.serviceRegion;
        }
        if (configuration.botId == null) {
            configuration.botId = defaultConfiguration.botId;
        }
        if (configuration.userId == null) {
            configuration.userId = defaultConfiguration.userId;
        }
        if (configuration.userName == null) {
            configuration.userName = defaultConfiguration.userName;
        }
        if (configuration.locale == null) {
            configuration.locale = defaultConfiguration.locale;
        }

        if (configuration.appCenterId == null)
        {
            configuration.appCenterId = defaultConfiguration.appCenterId;
        }
        return configuration;
    }


}
