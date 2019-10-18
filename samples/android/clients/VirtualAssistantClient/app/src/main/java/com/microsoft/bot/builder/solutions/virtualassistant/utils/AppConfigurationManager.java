package com.microsoft.bot.builder.solutions.virtualassistant.utils;

import android.content.Context;
import android.content.SharedPreferences;
import android.support.v4.content.ContextCompat;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.bot.builder.solutions.virtualassistant.R;

import java.io.IOException;
import java.io.InputStream;

public class AppConfigurationManager {

    // CONSTANTS
    final private static String SHARED_PREFS_NAME = "va_shared_prefs";
    final private static String SHARED_PREFS_CONFIGURATION = "va_app_configuration";
    final private static String DEFAULT_APP_CONFIGURATION_FILE = "default_app_configuration.json";
    final private static String LOGTAG = "AppConfigurationManager";

    // STATE
    private Context context;
    private SharedPreferences sharedPreferences;
    private Gson gson;
    private AppConfiguration defaultConfiguration;

    public AppConfigurationManager(Context context) {
        this.context = context;
        sharedPreferences = context.getSharedPreferences(SHARED_PREFS_NAME, Context.MODE_PRIVATE);
        gson = new Gson();
        try {
            InputStream is = context.getAssets().open(DEFAULT_APP_CONFIGURATION_FILE);
            int size = is.available();
            byte[] buffer = new byte[size];
            is.read(buffer);
            is.close();
            String jsonString = new String(buffer, "UTF-8");
            defaultConfiguration = gson.fromJson(jsonString, new TypeToken<AppConfiguration>(){}.getType());
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public void setConfiguration(AppConfiguration appConfiguration){
        SharedPreferences.Editor editor = sharedPreferences.edit();
        String json = gson.toJson(appConfiguration);
        editor.putString(SHARED_PREFS_CONFIGURATION, json);
        editor.apply();
    }

    public AppConfiguration getConfiguration(){
        String json = sharedPreferences.getString(SHARED_PREFS_CONFIGURATION, "{}");
        AppConfiguration appConfiguration = gson.fromJson(json, new TypeToken<AppConfiguration>(){}.getType());
        if (appConfiguration.historyLinecount == null) {
            appConfiguration.historyLinecount = defaultConfiguration.historyLinecount;
        }
        if (appConfiguration.colorBubbleBot == null) {
            appConfiguration.colorBubbleBot = ContextCompat.getColor(context, R.color.color_chat_background_bot);
        }
        if (appConfiguration.colorTextBot == null) {
            appConfiguration.colorTextBot = ContextCompat.getColor(context, R.color.color_chat_text_bot);
        }
        if (appConfiguration.colorBubbleUser == null) {
            appConfiguration.colorBubbleUser = ContextCompat.getColor(context, R.color.color_chat_background_user);
        }
        if (appConfiguration.colorTextUser == null) {
            appConfiguration.colorTextUser = ContextCompat.getColor(context, R.color.color_chat_text_user);
        }
        if (appConfiguration.showFullConversation == null) {
            appConfiguration.showFullConversation = defaultConfiguration.showFullConversation;
        }
        if (appConfiguration.enableDarkMode == null) {
            appConfiguration.enableDarkMode = defaultConfiguration.enableDarkMode;
        }
        if (appConfiguration.keepScreenOn == null) {
            appConfiguration.keepScreenOn = defaultConfiguration.keepScreenOn;
        }

        if (appConfiguration.appCenterId == null)
        {
            appConfiguration.appCenterId = defaultConfiguration.appCenterId;
        }
        return appConfiguration;
    }
}
