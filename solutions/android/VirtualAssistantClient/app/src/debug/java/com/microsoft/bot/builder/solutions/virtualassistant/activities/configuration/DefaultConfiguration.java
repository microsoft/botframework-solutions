package com.microsoft.bot.builder.solutions.virtualassistant.activities.configuration;

public class DefaultConfiguration {

    // Replace below with your own subscription key
    public static final String SPEECH_SERVICE_SUBSCRIPTION_KEY = "ccbc14f1bb2a476187a831c1dcc6a3fc";

    // Ryan's latest Bot : calendar, email, to-do, and point of interest skills
    public static final String DIRECT_LINE_SPEECH_SECRET_KEY = "aeRqQEhGPaM.cwA.TkU.3pFGkwEK7X8m4IxFujVOhURSByvHEuVVi1Na_u8yBHI";//aeRqQEhGPaM.cwA.TkU.3pFGkwEK7X8m4IxFujVOhURSByvHEuVVi1Na_u8yBHI

    // Replace below with your own service region (e.g., "westus").
    public static final String SPEECH_SERVICE_SUBSCRIPTION_KEY_REGION = "westus2";

    public static final String USER_NAME = "User";
    public static final String USER_FROM_ID = "625d6da6-1862-452b-b0ba-f2fa1ea5a7c5";

    public static final String LOCALE = "en-us"; //note: attempted to use Locale.getDefault().toString(); however it gives a string the bot doesn't recognize
}