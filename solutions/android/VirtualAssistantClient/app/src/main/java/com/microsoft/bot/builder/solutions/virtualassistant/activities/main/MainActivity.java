package com.microsoft.bot.builder.solutions.virtualassistant.activities.main;

import android.Manifest;
import android.app.assist.AssistContent;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.media.AudioManager;
import android.media.MediaPlayer;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.provider.Settings;
import android.support.design.widget.NavigationView;
import android.support.design.widget.TextInputEditText;
import android.support.design.widget.TextInputLayout;
import android.support.v4.app.ActivityCompat;
import android.support.v4.view.GravityCompat;
import android.support.v4.widget.DrawerLayout;
import android.support.v7.app.ActionBarDrawerToggle;
import android.support.v7.widget.LinearLayoutManager;
import android.support.v7.widget.RecyclerView;
import android.support.v7.widget.SwitchCompat;
import android.util.Log;
import android.view.KeyEvent;
import android.view.MenuItem;
import android.view.View;
import android.view.inputmethod.EditorInfo;
import android.widget.CompoundButton;
import android.widget.ImageView;
import android.widget.RelativeLayout;
import android.widget.TextView;

import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.botconfiguration.BotConfigurationActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.configuration.AppConfigurationActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.list.ChatAdapter;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.list.ItemOffsetDecoration;
import com.microsoft.bot.builder.solutions.virtualassistant.assistant.VoiceInteractionActivity;

import org.greenrobot.eventbus.EventBus;
import org.greenrobot.eventbus.Subscribe;
import org.greenrobot.eventbus.ThreadMode;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnCheckedChanged;
import butterknife.OnClick;
import butterknife.OnEditorAction;
import client.model.BotConnectorActivity;
import events.ActivityReceived;
import events.Disconnected;
import events.Recognized;
import events.RecognizedIntermediateResult;

public class MainActivity extends BaseActivity implements NavigationView.OnNavigationItemSelectedListener {

    // VIEWS
    @BindView(R.id.root_container) RelativeLayout uiContainer;
    @BindView(R.id.recyclerview) RecyclerView chatRecyclerView;
    @BindView(R.id.textinputlayout) TextInputLayout textInputLayout;
    @BindView(R.id.textinput) TextInputEditText textInput;
    @BindView(R.id.drawer_layout) DrawerLayout drawer;
    @BindView(R.id.nav_view) NavigationView navigationView;
    @BindView(R.id.switch_show_textinput) SwitchCompat switchShowTextInput;
    @BindView(R.id.speech_detection) TextView detectedSpeechToText;
    @BindView(R.id.agent_image) ImageView agentImage;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_main;
    private static final String LOGTAG = "MainActivity";

    // STATE
    private ChatAdapter chatAdapter;
    private boolean alwaysShowTextInput;
    private Handler handler;
    private boolean launchedAsAssistant;


    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);

        handler = new Handler(Looper.getMainLooper());


        // Options hidden in the nav-drawer
        alwaysShowTextInput = getBooleanSharedPref(SHARED_PREF_SHOW_TEXTINPUT);
        switchShowTextInput.setChecked(alwaysShowTextInput);

        // NAV DRAWER
        ActionBarDrawerToggle toggle = new ActionBarDrawerToggle( this, drawer, R.string.navigation_drawer_open, R.string.navigation_drawer_close);
        drawer.addDrawerListener(toggle);
        toggle.syncState();
        navigationView.setNavigationItemSelectedListener(this);

        // handle dangerous permissions
        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
            requestRecordAudioPermissions();
        } else {
            if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
                requestFineLocationPermissions();
            }
        }

        // make media volume the default
        setVolumeControlStream(AudioManager.STREAM_MUSIC);

        setupChatRecyclerView();

        // check if this activity was launched as an assistant
        Intent intent = getIntent();
        if (intent != null) {
            String originator = intent.getStringExtra(VoiceInteractionActivity.KEY_ORIGINATOR);
            if (originator != null && originator.equals(VoiceInteractionActivity.KEY_VALUE)) {
                launchedAsAssistant = true;//this flag can now be used, i.e. to automtically start microphone recording
            }
        }

    }

    // Register for EventBus messages and SpeechService
    @Override
    public void onStart() {
        super.onStart();
        EventBus.getDefault().register(this);
        if (speechServiceBinder == null) {
            doBindService();
        }
    }

    @Override
    protected void onResume() {
        super.onResume();
        final ConfigurationManager configurationManager = new ConfigurationManager(this);
        final Configuration configuration = configurationManager.getConfiguration();
        chatAdapter.setChatItemHistoryCount(configuration.historyLinecount==null?1:configuration.historyLinecount);
    }

    // Unregister EventBus messages and SpeechService
    @Override
    public void onStop() {
        Log.v("BaseActivity","onStop() finished");
        EventBus.getDefault().unregister(this);
        if (speechServiceBinder != null) {
            unbindService(myConnection);
            speechServiceBinder = null;
        }
        super.onStop();
    }

    private void setupChatRecyclerView() {

        chatAdapter = new ChatAdapter();
        chatRecyclerView.setAdapter(chatAdapter);

        LinearLayoutManager layoutManager = new LinearLayoutManager(this);
        layoutManager.setStackFromEnd(true);
        chatRecyclerView.setLayoutManager(layoutManager);

        final int spacing = getResources().getDimensionPixelOffset(R.dimen.list_item_spacing_small);
        chatRecyclerView.addItemDecoration(new ItemOffsetDecoration(spacing));
    }

    @Override
    protected void permissionDenied(String manifestPermission) {
        if (manifestPermission.equals(Manifest.permission.RECORD_AUDIO)){
            speechServiceBinder.initializeSpeechSdk(false);
            speechServiceBinder.getSpeechSdk().connectAsync();
            agentImage.setVisibility(View.GONE);//hide the assistant since voice is deactivated
            textInputLayout.setVisibility(View.VISIBLE);// show the text-input prompt
        }
    }

    @Override
    protected void permissionGranted(String manifestPermission) {
        // this code is triggered when a user launches app a 1st time and doesn't have permisison yet
        if (manifestPermission.equals(Manifest.permission.RECORD_AUDIO)){
            if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
                requestFineLocationPermissions();
            } else {
                initializeAndConnect();
            }
        }
        if (manifestPermission.equals(Manifest.permission.ACCESS_FINE_LOCATION)){
            if (speechServiceBinder != null) speechServiceBinder.startLocationUpdates();
            initializeAndConnect();
        }
    }

    @Override
    protected void serviceConnected() {
        // this code is triggered when a user launches the app a second+ time and the app has permission
        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED) {
            initializeAndConnect();
        }
    }

    @Override
    public boolean onNavigationItemSelected(MenuItem item) {
        int id = item.getItemId();

        switch (id) {
            case R.id.nav_menu_configuration:
                startActivity(BotConfigurationActivity.getNewIntent(this));
                break;
            case R.id.nav_menu_app_configuration:
                startActivity(AppConfigurationActivity.getNewIntent(this));
                break;
            case R.id.nav_menu_reset_bot:
                speechServiceBinder.getSpeechSdk().resetBot();
                break;
            case R.id.nav_menu_location:
                Configuration configuration = speechServiceBinder.getConfiguration();
                speechServiceBinder.getSpeechSdk().sendLocationEvent(configuration.geolat, configuration.geolon);
                break;
            case R.id.nav_menu_welcome_req:
                speechServiceBinder.getSpeechSdk().requestWelcomeCard();
                break;
            case R.id.nav_menu_emulate_activity_msg:
                final String testJson =
                        "{\"attachmentLayout\":\"carousel\",\"attachments\":[{\"content\":{\"body\":[{\"items\":[{\"columns\":[{\"items\":[{\"color\":\"accent\",\"id\":\"Name\",\"separation\":\"none\",\"size\":\"large\",\"spacing\":\"none\",\"text\":\"City Center Plaza\",\"type\":\"TextBlock\",\"weight\":\"bolder\"},{\"id\":\"AvailableDetails\",\"isSubtle\":true,\"separation\":\"none\",\"spacing\":\"none\",\"text\":\"Parking Garage\",\"type\":\"TextBlock\"},{\"color\":\"dark\",\"id\":\"Address\",\"isSubtle\":true,\"separation\":\"none\",\"spacing\":\"none\",\"text\":\"474 108th Avenue Northeast, Bellevue, West Bellevue\",\"type\":\"TextBlock\",\"wrap\":true},{\"color\":\"dark\",\"id\":\"Hours\",\"isSubtle\":true,\"separation\":\"none\",\"spacing\":\"none\",\"text\":\"\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"Column\",\"verticalContentAlignment\":\"Center\",\"width\":\"90\"}],\"type\":\"ColumnSet\"}],\"type\":\"Container\"},{\"items\":[{\"id\":\"Image\",\"type\":\"Image\",\"url\":\"https://atlas.microsoft.com/map/static/png?api-version=1.0&layer=basic&style=main&zoom=15&center=-122.19475,47.61426&width=512&height=512&subscription-key=X0_-LfxI-A-iXxsBGb62ZZJfdfr5mbw9LiG8-cL6quM\"}],\"separator\":true,\"type\":\"Container\"}],\"id\":\"PointOfInterestViewCard\",\"speak\":\"City Center Plaza at 474 108th Avenue Northeast\",\"type\":\"AdaptiveCard\",\"version\":\"1.0\"},\"contentType\":\"application/vnd.microsoft.card.adaptive\"},{\"content\":{\"body\":[{\"items\":[{\"columns\":[{\"items\":[{\"color\":\"accent\",\"id\":\"Name\",\"separation\":\"none\",\"size\":\"large\",\"spacing\":\"none\",\"text\":\"Plaza Center\",\"type\":\"TextBlock\",\"weight\":\"bolder\"},{\"id\":\"AvailableDetails\",\"isSubtle\":true,\"separation\":\"none\",\"spacing\":\"none\",\"text\":\"Parking Garage\",\"type\":\"TextBlock\"},{\"color\":\"dark\",\"id\":\"Address\",\"isSubtle\":true,\"separation\":\"none\",\"spacing\":\"none\",\"text\":\"10901 NE 9th St, Bellevue, Northwest Bellevue\",\"type\":\"TextBlock\",\"wrap\":true},{\"color\":\"dark\",\"id\":\"Hours\",\"isSubtle\":true,\"separation\":\"none\",\"spacing\":\"none\",\"text\":\"\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"Column\",\"verticalContentAlignment\":\"Center\",\"width\":\"90\"}],\"type\":\"ColumnSet\"}],\"type\":\"Container\"},{\"items\":[{\"id\":\"Image\",\"type\":\"Image\",\"url\":\"https://atlas.microsoft.com/map/static/png?api-version=1.0&layer=basic&style=main&zoom=15&center=-122.19493,47.61793&width=512&height=512&subscription-key=X0_-LfxI-A-iXxsBGb62ZZJfdfr5mbw9LiG8-cL6quM\"}],\"separator\":true,\"type\":\"Container\"}],\"id\":\"PointOfInterestViewCard\",\"speak\":\"Plaza Center at 10901 NE 9th St\",\"type\":\"AdaptiveCard\",\"version\":\"1.0\"},\"contentType\":\"application/vnd.microsoft.card.adaptive\"}],\"channelData\":{\"conversationalAiData\":{\"requestInfo\":{\"interactionId\":\"b9ad8f12-e459-4a73-a542-919224e83b0a\",\"requestType\":0,\"version\":\"0.2\"}}},\"channelId\":\"directlinespeech\",\"conversation\":{\"id\":\"490b89e7-ab99-4ec6-b0c8-4cc612d5e4ce\",\"isGroup\":false},\"entities\":[],\"from\":{\"id\":\"vakonadj\"},\"id\":\"a27c2f8da5a845a3942f6a880562114f\",\"inputHint\":\"expectingInput\",\"recipient\":{\"id\":\"490b89e7-ab99-4ec6-b0c8-4cc612d5e4ce|0000\"},\"replyToId\":\"c3174265-3b0a-49a1-bdb1-e55c477b8c36\",\"serviceUrl\":\"PersistentConnection\",\"speak\":\"What do you think of these?,1 - City Center Plaza at 474 108th Avenue Northeast.,2 - Plaza Center at 10901 NE 9th St.\",\"text\":\"What do you think of these?\",\"timestamp\":\"2019-04-25T18:17:12.3964213+00:00\",\"type\":\"message\"}";
                speechServiceBinder.getSpeechSdk().activityReceived(testJson);
                break;
            case R.id.nav_menu_show_assistant_settings:
                startActivity(new Intent(Settings.ACTION_VOICE_INPUT_SETTINGS));
                break;
        }

        drawer.closeDrawer(GravityCompat.START);

        return true;
    }

    @OnClick(R.id.agent_image)
    public void onAssistantClick() {
        showSnackbar(uiContainer, getString(R.string.msg_listening));
        speechServiceBinder.getSpeechSdk().listenOnceAsync();
    }

    @OnCheckedChanged(R.id.switch_show_textinput)
    public void OnShowTextInput(CompoundButton button, boolean checked){
        alwaysShowTextInput = checked;
        putBooleanSharedPref(SHARED_PREF_SHOW_TEXTINPUT, checked);
        if (alwaysShowTextInput)
            textInputLayout.setVisibility(View.VISIBLE);
        else
            textInputLayout.setVisibility(View.GONE);
    }

    @OnEditorAction(R.id.textinput)
    boolean onEditorAction(int actionId, KeyEvent key){
        boolean handled = false;
        if (actionId == EditorInfo.IME_ACTION_SEND || (key != null && key.getKeyCode() == KeyEvent.KEYCODE_ENTER)) {
            sendTextMessage(textInput.getEditableText().toString());
            textInput.setText("");
            hideKeyboardFrom(textInput);
            handled = true;
        }
        return handled;
    }

    // send text message
    private void sendTextMessage(String msg){
        speechServiceBinder.getSpeechSdk().sendActivityMessageAsync(msg);
    }

    // EventBus: the user spoke and the app recognized intermediate speech
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onDisconnectedEvent(Disconnected event) {
        detectedSpeechToText.setText(R.string.msg_disconnected);
        boolean havePermission = ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED;
        speechServiceBinder.initializeSpeechSdk(havePermission);
        speechServiceBinder.getSpeechSdk().connectAsync();
        handler.postDelayed(() -> {
            detectedSpeechToText.setText("");
        }, 2000);
    }

    // EventBus: the user spoke and the app recognized intermediate speech
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onRecognizedIntermediateResultEvent(RecognizedIntermediateResult event) {
        detectedSpeechToText.setText(event.recognized_speech);
    }

    // EventBus: the user spoke and the app recognized the speech. Disconnect mic.
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onRecognizedEvent(Recognized event) {
        detectedSpeechToText.setText(event.recognized_speech);
        // in 2 seconds clear the text (at this point the bot should be giving its' response)
        handler.postDelayed(() -> detectedSpeechToText.setText(""), 2000);
    }

    // EventBus: received a response from Bot
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onActivityReceivedEvent(ActivityReceived activityReceived) throws IOException {
        if (activityReceived.botConnectorActivity != null) {
            BotConnectorActivity botConnectorActivity = activityReceived.botConnectorActivity;

            String amount;

            switch (botConnectorActivity.getType()) {
                case "message":
                    chatAdapter.addChat(botConnectorActivity, this);
                    // make the chat list scroll automatically after adding a bot response
                    chatRecyclerView.getLayoutManager().scrollToPosition(chatAdapter.getItemCount() - 1);
                    break;
                case "dialogState":
                    Log.i(LOGTAG, "Activity with DialogState");
                    break;
                case "PlayLocalFile":
                    Log.i(LOGTAG, "Activity with PlayLocalFile");
                    playMediaStream(botConnectorActivity.getFile());
                    break;
                default:
                    break;
            }
        }
    }

    private void playMediaStream(String mediaStream) {
        try {
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.setDataSource(mediaStream);
            mediaPlayer.prepare();
            mediaPlayer.start();
        }
        catch(IOException e) {
            Log.e(LOGTAG, "IOexception " + e.getMessage());
        }

    }

    // provide additional data to the Assistant
    // improve the assistant user experience by providing content-related references related to the current activity
    @Override
    public void onProvideAssistContent(AssistContent outContent) {
        super.onProvideAssistContent(outContent);
        //NOTE: to test the data you're passing in, see https://search.google.com/structured-data/testing-tool/u/0/
        // ALSO: see https://schema.org/ for the vocabulary

        // three examples :
//        outContent.setWebUri(
//                Uri.parse(
//                        "http://www.goodreads.com/book/show/13023.Alice_in_Wonderland"
//                )
//        );
//        outContent.setStructuredData(
//                new JSONObject()
//                        .put("@type", "Book")
//                        .put("author", "Lewis Carroll")
//                        .put("name", "Alice in Wonderland")
//                        .put("description",
//                                "This is an 1865 novel about a girl named Alice, " +
//                                        "who falls through a rabbit hole and " +
//                                        "enters a fantasy world."
//                        ).toString()
//        );

        try {
            String structuredJson = new JSONObject()
                    .put("@type", "MusicRecording")
                    .put("@id", "https://example.com/music/recording")
                    .put("name", "Album Title")
                    .put("description", "Album Description")
                    .toString();

            outContent.setStructuredData(structuredJson);
        } catch (JSONException e) {
            e.printStackTrace();
        }

    }
}
