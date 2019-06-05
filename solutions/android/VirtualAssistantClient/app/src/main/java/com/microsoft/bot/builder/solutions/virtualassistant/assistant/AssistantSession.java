package com.microsoft.bot.builder.solutions.virtualassistant.assistant;

import android.app.assist.AssistContent;
import android.app.assist.AssistStructure;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.net.Uri;
import android.os.Bundle;
import android.service.voice.VoiceInteractionSession;
import android.util.Log;

public class AssistantSession extends VoiceInteractionSession {

    // CONSTANTS
    static final String LOGTAG = "AssistantSession";

    // STATE
    private Context context;

    public AssistantSession(Context context) {
        super(context);
        this.context = context;
    }

    @Override
    public void onCreate() {
        super.onCreate();
        Log.i(LOGTAG, "onCreate");
    }

    @Override
    public void onShow(Bundle args, int showFlags) {
        super.onShow(args, showFlags);
        Log.i(LOGTAG, "onShow: flags=0x" + Integer.toHexString(showFlags) + " args=" + args);
   }

    @Override
    public void onHandleAssist(Bundle data, AssistStructure structure, AssistContent content) {
        super.onHandleAssist(data, structure, content);

        Log.i(LOGTAG, "onHandleAssist");
        logAssistContentAndData(content, data);

        Intent intent = new Intent(context, VoiceInteractionActivity.class);
        startVoiceActivity(intent);
    }

    private void logAssistContentAndData(AssistContent content, Bundle data) {
        if (content != null) {
            Log.i(LOGTAG, "Assist intent: " + content.getIntent());
            Log.i(LOGTAG, "Assist intent from app: " + content.isAppProvidedIntent());
            Log.i(LOGTAG, "Assist clipdata: " + content.getClipData());
            Log.i(LOGTAG, "Assist structured data: " + content.getStructuredData());
            Log.i(LOGTAG, "Assist web uri: " + content.getWebUri());
            Log.i(LOGTAG, "Assist web uri from app: " + content.isAppProvidedWebUri());
            Log.i(LOGTAG, "Assist extras: " + content.getExtras());
        }
        if (data != null) {
            Uri referrer = data.getParcelable(Intent.EXTRA_REFERRER);
            if (referrer != null) {
                Log.i(LOGTAG, "Referrer: " + referrer);
            }
        }
    }

    @Override
    public void onHandleScreenshot(Bitmap screenshot) {
        // this can provide a screenshot of the UI - check for null
    }

}
