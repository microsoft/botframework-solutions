package com.microsoft.bot.builder.solutions.virtualassistant.utils;

import android.content.ActivityNotFoundException;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;

public class PlayStoreUtils {

    public static void launchPlayStore(Context context, String namespace){
        Uri uri = Uri.parse("market://details?id=" + namespace);
        Intent goToMarket = new Intent(Intent.ACTION_VIEW, uri);
        // After pressing back button, to get taken back to our app, add following flags to intent
        goToMarket.addFlags(Intent.FLAG_ACTIVITY_NO_HISTORY |
                Intent.FLAG_ACTIVITY_NEW_DOCUMENT |
                Intent.FLAG_ACTIVITY_MULTIPLE_TASK);
        try {
            context.startActivity(goToMarket);
        } catch (ActivityNotFoundException e) {
            context.startActivity(new Intent(Intent.ACTION_VIEW,
                    Uri.parse("http://play.google.com/store/apps/details?id=" + namespace)));
        }
    }
}
