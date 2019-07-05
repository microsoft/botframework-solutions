package com.microsoft.bot.builder.solutions.virtualassistant.utils;

import android.content.Context;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

import java.io.IOException;
import java.io.InputStream;

/**
 * Collection of utilities to interact with res/raw
 */
public class RawUtils {

    public static String loadHostConfig(Context context) {
        String json = null;
        try {
            InputStream is = context.getResources().openRawResource(R.raw.hostconfig);
            int size = is.available();
            byte[] buffer = new byte[size];
            is.read(buffer);
            is.close();
            json = new String(buffer, "UTF-8");
        } catch (IOException ex) {
            ex.printStackTrace();
            return null;
        }
        return json;
    }
}
