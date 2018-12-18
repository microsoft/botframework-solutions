package com.microsoft.assistant_android;

import android.Manifest;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.drawable.ColorDrawable;
import android.os.Handler;
import android.os.Bundle;
import android.os.SystemClock;
import android.preference.PreferenceManager;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.PopupWindow;
import android.widget.TextView;
import android.widget.Toast;

import com.microsoft.assistant_android.speech.SpeechEventInterface;
import com.microsoft.assistant_android.speech.SpeechImpl;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.IOException;
import java.io.InputStream;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;

import io.adaptivecards.objectmodel.AdaptiveCard;
import io.adaptivecards.objectmodel.BaseActionElement;
import io.adaptivecards.objectmodel.BaseCardElement;
import io.adaptivecards.objectmodel.HostConfig;
import io.adaptivecards.objectmodel.ParseResult;
import io.adaptivecards.renderer.AdaptiveCardRenderer;
import io.adaptivecards.renderer.RenderedAdaptiveCard;
import io.adaptivecards.renderer.actionhandler.ICardActionHandler;

import static android.Manifest.permission.INTERNET;
import static android.Manifest.permission.RECORD_AUDIO;

public class MainActivity extends AppCompatActivity implements BotEventInterface, ICardActionHandler, SpeechEventInterface, View.OnClickListener {

    private static final String TAG = MainActivity.class.getSimpleName();
    Handler timerHandler = new Handler();
    Runnable timerRunnable = new Runnable() {
        @Override
        public void run() {
            ((TextView) findViewById(R.id.txtTime)).setText(getCurrentTimeUsingDate());
            timerHandler.postDelayed(this, 1000);
        }
    };
    Handler processHandler = new Handler();
    Runnable processRunnable = new Runnable() {
        @Override
        public void run() {
            if (!_bRecognizing) {
                if (!processingStack.isEmpty()) {
                    if (_bIsSpeechReady) {
                        String msg = processingStack.remove(0);
                        //Log.d(TAG, "Processing msg: " + msg);
                        ProcessMessage(msg);
                    }
//                    else
//                        Log.d(TAG,"Cant process becasue Speaking");
                }
//                else
//                    Log.d(TAG,"No msgs to process");
            }
//            else
//                Log.d(TAG,"Recognizing speech");
            processHandler.postDelayed(this,1000);
        }
    };
    private SpeechImpl _speech = null;
    private String _speak = "";
    private BotWrapper _bot;
    private boolean _bRecognizing = false;
    private boolean _bIsSpeechReady = false;
    private boolean bMenuOpen = false;

    private ArrayList<String> processingStack = new ArrayList<String>();
    private HostConfig _hostConfig;
    View popupContentView;
    PopupWindow popupWindow;
    // Get the app's shared preferences
    SharedPreferences _preferences;
    final String PREF_BACKGROUND     = "Background";
    final String PREF_BOT_SECRET     = "BotSecret";
    final String PREF_VOICE_KEY      = "VoiceKey";
    final String PREF_VOICE_REGION   = "VoiceRegion";
    final String PREF_VOICE_ENDPOINT = "VoiceEndpoint";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        setTheme(R.style.AppTheme_Launcher);
        super.onCreate(savedInstanceState);
        this.requestWindowFeature(Window.FEATURE_NO_TITLE);
        setContentView(R.layout.activity_main);
        CheckPermissions();

        //set up click listeners for menu
        findViewById(R.id.imgMenu).setOnClickListener(this);
        findViewById(R.id.speechReady).setOnClickListener(this);
        //set up click listeners for menu

        ((TextView) findViewById(R.id.txtTime)).setText(getCurrentTimeUsingDate());
        final Thread.UncaughtExceptionHandler oldHandler = Thread.getDefaultUncaughtExceptionHandler();

        Thread.setDefaultUncaughtExceptionHandler(
                new Thread.UncaughtExceptionHandler() {
                    @Override
                    public void uncaughtException(
                            Thread paramThread,
                            Throwable paramThrowable
                    ) {
                        //Do your own error handling here

                        if (oldHandler != null)
                            oldHandler.uncaughtException(
                                    paramThread,
                                    paramThrowable
                            ); //Delegates to Android's error handling
                        else {
                            System.exit(2); //Prevents the service/app from freezing
                        }
                    }
                });
        _hostConfig = HostConfig.DeserializeFromString(loadHostConfig());

        //set up popup window
        popupWindow = new PopupWindow(getApplicationContext());
        popupContentView = LayoutInflater.from(this).inflate(R.layout.popup_view_layout, null);
        // Get the app's shared preferences
        _preferences = PreferenceManager.getDefaultSharedPreferences(this);
        InitializeSettingsView();

        processHandler.post(processRunnable);
    }
    @Override
    public void onClick(View v) {
        switch (v.getId()) {
            case R.id.imgMenu:
                onMenuClicked(v);
                break;
            case R.id.speechReady:
                if (_bIsSpeechReady) {
                    if (_speech != null)
                        _speech.StartSpeechReco();
                    else
                        Toast.makeText(this, "Speech recognition not started. Please enter key.", Toast.LENGTH_LONG).show();
                }
                break;
        }
    }
    private void CheckPermissions() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED ||
            ContextCompat.checkSelfPermission(this, Manifest.permission.INTERNET) != PackageManager.PERMISSION_GRANTED ) {
            int requestCode = 5; // unique code for the permission request
            // Note: we need to request audio permissions
            ActivityCompat.requestPermissions(MainActivity.this, new String[]{RECORD_AUDIO, INTERNET}, requestCode);
        }
    }
    private void ProcessMessage(String message) {
        try {
            JSONObject activity = new JSONObject(message);
            String type = activity.getString("type");
            String input = activity.optString("inputHint", "");
            String speak = activity.optString("speak","");
            if (input.contains("expectingInput"))
                _bot.BotState = _bot.BOT_STATE_EXPECTING_INPUT;
            else if (input.contains("acceptingInput"))
                _bot.BotState = _bot.BOT_STATE_ACCEPTING_INPUT;
            else if (input.contains("ignoringInput"))
                _bot.BotState = _bot.BOT_STATE_IGNORING_INPUT;
            else if (input.length() == 0)
                _bot.BotState = _bot.BOT_STATE_ACCEPTING_INPUT;
            else
                Log.d(TAG,"input hint is not what we expect: " + input);

            if (type.compareTo("message") == 0) {
                //((TextView) (getActivity().findViewById(R.id.txtSpeechDisplay))).setText("");
                String from = activity.getJSONObject("from").getString("id");
                //((TextView) getActivity().findViewById(R.id.textBot)).setText("");
                JSONArray attachmentsArray = activity.optJSONArray("attachments");
                if (attachmentsArray != null && attachmentsArray.length() > 0) {
                    Integer attachmentsArrayLength = attachmentsArray.length();
                    for (int x = 0; x < attachmentsArrayLength; x++) {
                        String card = attachmentsArray.getJSONObject(x).getString("content");
                        Log.d(TAG, "Recv: " + card);
                        ParseResult parseResult = AdaptiveCard.DeserializeFromString(card, AdaptiveCardRenderer.VERSION);
                        AdaptiveCard adaptiveCard = parseResult.GetAdaptiveCard();
                        RenderedAdaptiveCard renderedCard = AdaptiveCardRenderer.getInstance().render(this, getSupportFragmentManager(), adaptiveCard, this, _hostConfig);
                        LinearLayout layout = findViewById(R.id.layoutCard);
                        layout.removeAllViewsInLayout();
                        View v = renderedCard.getView();
                        layout.addView(v);
                    }
                } else {
                    String text = activity.getString("text");
                    LinearLayout layout = findViewById(R.id.layoutCard);
                    layout.removeAllViewsInLayout();
                    TextView lblTV = new TextView(this);
                    lblTV.setText("Returned text: " + text);
                    lblTV.setLayoutParams(new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT));
                    lblTV.setTextColor(Color.parseColor("#000000"));
                    lblTV.setTextSize(16f);
                    lblTV.setGravity(Gravity.CENTER);
                    layout.addView(lblTV);
                }
                if (speak.length() > 0) {
                    _speech.Speak(speak);
                    ((ImageView) findViewById(R.id.speechReady)).setImageResource(R.drawable.yellow);
                }
                else
                    ((ImageView) findViewById(R.id.speechReady)).setImageResource(R.drawable.green);
            }
        }
        catch (Exception ex) {
            Log.e(TAG,"Error in process message: " + ex.getMessage());
        }
    }

    public void StartConversation() {
        _bot = new BotWrapper(this, _preferences.getString(PREF_BOT_SECRET, ""));
        //set up speech
        if (_speech == null) {
            String voiceKey = _preferences.getString(PREF_VOICE_KEY, "");
            String voiceRegion = _preferences.getString(PREF_VOICE_REGION, "westus");
            String voiceEndpoint = _preferences.getString(PREF_VOICE_ENDPOINT, "");
            if (voiceKey.length() > 0 && voiceRegion.length() > 0)
                _speech = new SpeechImpl(getApplicationContext(), this, R.raw.beep, voiceKey, voiceRegion, voiceEndpoint);
            else
                Toast.makeText(this,"Voice Recognition Key and/or Voice Region is blank", Toast.LENGTH_LONG).show();
        }
    }

    public void EndConversation() {
        try {
            _bot.EndConversation();
            _bot = null;
        }
        catch (IllegalStateException ex) {
            Toast.makeText(this,"Bot not started. Most likely cause is incorrect Bot Secret",Toast.LENGTH_LONG).show();
        }
        LinearLayout layout = findViewById(R.id.layoutCard);
        layout.removeAllViews();
        _speech = null;
    }

    public void onMenuClicked(View v) {
        if (!bMenuOpen) {
            // Create popup window.
            popupWindow = new PopupWindow(popupContentView, ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT);
            popupWindow.setOnDismissListener(new PopupWindow.OnDismissListener() {
                @Override
                public void onDismiss() {
                    SaveSettingsValues();
                    bMenuOpen = !bMenuOpen;
                }
            });

            // Set popup window animation style.
            popupWindow.setAnimationStyle(R.style.popup_window_animation);
            popupWindow.setContentView(popupContentView);
            popupWindow.setBackgroundDrawable(new ColorDrawable(Color.WHITE));

            popupWindow.setFocusable(true);

            popupWindow.setHeight(500);
            popupWindow.setWidth(700);
            popupWindow.setOutsideTouchable(true);

            popupWindow.update();
            popupWindow.showAtLocation(findViewById(R.id.layoutMain), Gravity.START|Gravity.TOP, 1, 1);

        } else {
            popupWindow.dismiss();
        }
        bMenuOpen = !bMenuOpen;
    }

    private void InitializeSettingsView() {
        Button cmdEnd = popupContentView.findViewById(R.id.cmdEndApp);
        cmdEnd.setOnClickListener(new Button.OnClickListener() {
            @Override
            public void onClick(View v) {
                finish();
                System.exit(0);
            }
        });

        final Button cmdConversation = popupContentView.findViewById(R.id.cmdConversation);
        cmdConversation.setOnClickListener(new Button.OnClickListener() {
            @Override
            public void onClick(View v) {
                if (cmdConversation.getText().toString().equals("Start Conversation")) {
                    SaveSettingsValues();
                    StartConversation();
                    cmdConversation.setText("End Conversation");
                } else if (cmdConversation.getText().toString().equals("End Conversation")) {
                    EndConversation();
                    cmdConversation.setText("Start Conversation");
                }
                popupWindow.dismiss();
            }
        });

        EditText txtBotSecret = popupContentView.findViewById(R.id.txtBotSecret);
        txtBotSecret.setText(_preferences.getString(PREF_BOT_SECRET,""));

        EditText txtBackground = popupContentView.findViewById(R.id.txtBackground);
        txtBackground.setText(_preferences.getString(PREF_BACKGROUND,"#FFFFFF"));

        EditText txtVoiceKey = popupContentView.findViewById(R.id.txtVoiceKey);
        txtVoiceKey.setText(_preferences.getString(PREF_VOICE_KEY,""));

        EditText txtVoiceRegion = popupContentView.findViewById(R.id.txtVoiceRegion);
        txtVoiceRegion.setText(_preferences.getString(PREF_VOICE_REGION,"westus"));

        EditText txtVoiceEndpoint = popupContentView.findViewById(R.id.txtVoiceEndpoint);
        txtVoiceEndpoint.setText(_preferences.getString(PREF_VOICE_ENDPOINT,""));

        try {
            findViewById(R.id.layoutMain).setBackgroundColor(Color.parseColor(txtBackground.getText().toString()));
        }
        catch (Exception ex) {
            Toast.makeText(this,"Error setting background. Make sure format is #RRGGBB",Toast.LENGTH_LONG).show();
        }
    }

    private void SaveSettingsValues() {
        SharedPreferences.Editor editor = _preferences.edit();

        EditText txtBotSecret = popupContentView.findViewById(R.id.txtBotSecret);
        editor.putString(PREF_BOT_SECRET,txtBotSecret.getText().toString());

        try {
            EditText txtBackground = popupContentView.findViewById(R.id.txtBackground);
            String color = txtBackground.getText().toString();
            editor.putString(PREF_BACKGROUND, txtBackground.getText().toString());
            findViewById(R.id.layoutMain).setBackgroundColor(Color.parseColor(color));
        }
        catch (Exception ex) {
            Toast.makeText(this,"Error setting background. Make sure format is #RRGGBB",Toast.LENGTH_LONG).show();
        }

        EditText txtVoiceKey = popupContentView.findViewById(R.id.txtVoiceKey);
        editor.putString(PREF_VOICE_KEY,txtVoiceKey.getText().toString());

        EditText txtVoiceRegion = popupContentView.findViewById(R.id.txtVoiceRegion);
        editor.putString(PREF_VOICE_REGION,txtVoiceRegion.getText().toString());

        EditText txtVoiceEndpoint = popupContentView.findViewById(R.id.txtVoiceEndpoint);
        editor.putString(PREF_VOICE_ENDPOINT,txtVoiceEndpoint.getText().toString());

        editor.commit();
    }

    public String getCurrentTimeUsingDate() {
        Date date = new Date();
        String strDateFormat = "h:mm aa";
        DateFormat dateFormat = new SimpleDateFormat(strDateFormat);
        String formattedDate= dateFormat.format(date);
        return formattedDate;
    }

    public String loadHostConfig() {
        String json = null;
        try {
            InputStream is = this.getResources().openRawResource(R.raw.hostconfig);
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
    //BotEventInterface implementation

    @Override
    public void onMessageReceived(String message) {
        try {
            JSONObject jsonObject = new JSONObject(message);
            Integer arrayLength = jsonObject.getJSONArray("activities").length();
            JSONArray activitiesArray = jsonObject.getJSONArray("activities");
            if (arrayLength > 0) {
                for (int i = 0; i < arrayLength; i++) {
                    JSONObject activity = activitiesArray.getJSONObject(i);
                    processingStack.add(activity.toString());
                }
            }
        }
        catch (Exception e) {
            e.printStackTrace();
        }
    }

    @Override
    public void onBotReady() {
        _bIsSpeechReady = true;
    }

    @Override
    public void onBotError(String error) {
        Toast.makeText(this,"Bot error: " + error,Toast.LENGTH_LONG).show();
        _bIsSpeechReady = true;
    }

    //BotEventInterface implementation

    //ICardActionHandler implementation
    @Override
    public void onAction(BaseActionElement actionElement, RenderedAdaptiveCard renderedAdaptiveCard) {
    }

    @Override
    public void onMediaPlay(BaseCardElement mediaElement, RenderedAdaptiveCard renderedAdaptiveCard) {

    }

    @Override
    public void onMediaStop(BaseCardElement mediaElement, RenderedAdaptiveCard renderedAdaptiveCard) {

    }
    //ICardActionHandler implementation

    //speech interface
    @Override
    public void onSpeechRecognizing(String text) {
        runOnUiThread(new Runnable() {
            public void run() {
                try {
                    //((TextView)(getActivity().findViewById(R.id.txtSpeechDisplay))).setText(text);
                }
                catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    @Override
    public void onSpeechRecognized(String text) {
        _bRecognizing = false;
        _bIsSpeechReady = true;
        runOnUiThread(new Runnable() {
            public void run() {
                try {
                    ((ImageView) findViewById(R.id.speechReady)).setImageResource(R.drawable.yellow);
                    _bot.SendMessage(text);
                }
                catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    @Override
    public void onDoneSpeaking() {
        _bIsSpeechReady = true;
        runOnUiThread(new Runnable() {
            public void run() {
                ((ImageView) findViewById(R.id.speechReady)).setImageResource(R.drawable.green);
            }
        });
    }

    @Override
    public void onInitialized() {
        runOnUiThread(new Runnable() {
            public void run() {
                ((ImageView) findViewById(R.id.speechReady)).setImageResource(R.drawable.green);
            }
        });
    }

    @Override
    public void onSpeechError(String utterandeId) {
        Log.e(TAG,"Error occured speaking (utteranceId: " + utterandeId);
    }

    @Override
    public void onSpeechRecoError(String message) {
        Log.e(TAG,"Error occurred in speech reco: " + message);
        Toast.makeText(this,"Speech reco error",Toast.LENGTH_LONG).show();
        _bRecognizing = false;
        _bIsSpeechReady = true;
    }
    //speech interface

}
