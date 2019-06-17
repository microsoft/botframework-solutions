package com.microsoft.bot.builder.solutions.virtualassistant.activities.configuration;

public class DefaultConfiguration {

    // Replace below with your own subscription key
    public static final String COGNITIVE_SERVICES_SUBSCRIPTION_KEY = "ccbc14f1bb2a476187a831c1dcc6a3fc";

    // Ryan's Bot
//    public static final String BOT_ID = "cJ1RDYg_dU0.cwA.ZM8.UTiNfcH2IkSqRhy2ld_qvMdLqQOoA_oyajszDZg5uRQ";

    // Ryan's new Bot
    //public static final String BOT_ID = "hkXlowGfWV8.cwA.y2o.qOWZfvxO2y5qdwTyEOJ_ouBa6EnUeWKUKKPcOTigtZ4";

    // Ryan's latest Bot : calendar, email, to-do, and point of interest skills
    public static final String BOT_ID = "aeRqQEhGPaM.cwA.TkU.3pFGkwEK7X8m4IxFujVOhURSByvHEuVVi1Na_u8yBHI";

    // VA Bot (is quite talkative). Replace below with your own Bot ID (from the Speech Channel configuration page)
    //public static final String BOT_ID = "PD0oNSmMjxs.cwA.iCY.cCCtCzYsccS8ViGCOO5Kg3H_6kDn4seaoAkcKECr-Y0";
    //public static final String BotId = "wss://vakonadj.azurewebsites.net/api/messages";

    //tahiti Demo bot (Khuram's creation)
    //public static final String BOT_ID = "lmcWAIfP2cg.cwA.d1w.7T31qMiS0RPYCljDjsdTT2phDKGfF6qL7XDtuFWbh-o";


    // tahiti BBC bot (this bot has the play radio ability but is otherwise dumb)
    //public static final String BOT_ID = "wss://drueservice1.azurewebsites.net/api/messages/1bc1f588-9926-7a84-302f-c81db70b981e";

    // tahiti Command bot (car commanding bot; has the ability to change temperature but is otherwise dumb)
    //public static final String BOT_ID = "wss://drueservice1.azurewebsites.net/api/messages/095a7427-73eb-38d1-fa55-60414005bbdf";

    // Replace below with your own service region (e.g., "westus").
    public static final String SPEECH_REGION = "westus2";

    public static final String VOICE_NAME = "Microsoft Server Speech Text to Speech Voice (en-US, JessaNeural)";

    public static final String DIRECT_LINE_CONSTANT = "directline";
    public static final String USER_NAME = "User";
    public static final String USER_ID = "625d6da6-1862-452b-b0ba-f2fa1ea5a7c5";

    public static final String LOCALE = "en-us"; //note: attempted to use Locale.getDefault().toString(); however it gives a string the bot doesn't recognize

    public static final String GEOLOCATION_LAT = "47.617305";
    public static final String GEOLOCATION_LON = "-122.192217";

    public static final String KEYWORD = "computer";
}
