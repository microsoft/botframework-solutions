package com.microsoft.bot.builder.solutions.virtualassistant.assistant;

import android.content.ComponentName;
import android.content.Intent;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;

import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.MainActivity;

/**
 * this class is used to re-launch the LAUNCH activity
 * Also, a special flag is added to let the MainActivity identify it's launched as an assistant
 */
public class VoiceInteractionActivity extends AppCompatActivity {

    // CONSTANTS
    public static final String KEY_ORIGINATOR = "KEY_ORIGINATOR";
    public static final String KEY_VALUE = "VoiceInteractionActivityFlag";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        Intent intent = new Intent(Intent.ACTION_MAIN);
        intent.addCategory(Intent.CATEGORY_LAUNCHER);
        intent.setComponent(new ComponentName(this, MainActivity.class));
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        intent.putExtra(KEY_ORIGINATOR, KEY_VALUE);//special flag
        startActivity(intent);
    }
}
