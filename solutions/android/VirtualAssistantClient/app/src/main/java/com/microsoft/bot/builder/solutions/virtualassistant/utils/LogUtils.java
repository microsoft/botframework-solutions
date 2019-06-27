package com.microsoft.bot.builder.solutions.virtualassistant.utils;

import android.util.Log;

public class LogUtils {

    public static void logLongInfoMessage(String tag, String message){
        // Split by line, then ensure each line can fit into Log's maximum length.
        final int MAX_LOG_LENGTH = 4000;
        for (int i = 0, length = message.length(); i < length; i++) {
            int newline = message.indexOf('\n', i);
            newline = newline != -1 ? newline : length;
            do {
                int end = Math.min(newline, i + MAX_LOG_LENGTH);
                Log.i(tag, message.substring(i, end));
                i = end;
            } while (i < newline);
        }
    }
}
