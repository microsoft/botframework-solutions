package com.microsoft.bot.builder.solutions.directlinespeech.utils;

import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class DateUtils {

    public static String getCurrentTime() {
        Date date = new Date();
        String strDateFormat = "hh:mm:ss a MM-dd-yyyy";
        DateFormat dateFormat = new SimpleDateFormat(strDateFormat, Locale.US);
        return dateFormat.format(date);
    }
}
