package com.microsoft.bot.builder.solutions.virtualassistant.assistant;

import android.content.ComponentName;
import android.content.Intent;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;

import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.MainActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.service.SpeechService;

import static com.microsoft.bot.builder.solutions.virtualassistant.service.SpeechService.ACTION_START_LISTENING;

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

        Intent intent = new Intent(this, SpeechService.class);
        intent.setAction(ACTION_START_LISTENING);
        startService(intent);
        finish();
    }
}
