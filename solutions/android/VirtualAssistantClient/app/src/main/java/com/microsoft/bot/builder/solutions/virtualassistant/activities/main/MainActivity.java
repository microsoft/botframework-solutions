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
import android.os.RemoteException;
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

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.actionslist.ActionsAdapter;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.actionslist.ActionsViewholder;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist.ChatAdapter;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist.ItemOffsetDecoration;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist.ViewholderBot;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.settings.SettingsActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.assistant.VoiceInteractionActivity;

import org.greenrobot.eventbus.EventBus;
import org.greenrobot.eventbus.Subscribe;
import org.greenrobot.eventbus.ThreadMode;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;
import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnCheckedChanged;
import butterknife.OnClick;
import butterknife.OnEditorAction;
import client.model.BotConnectorActivity;
import client.model.CardAction;
import events.ActivityReceived;
import events.Disconnected;
import events.Recognized;
import events.RecognizedIntermediateResult;
import events.RequestTimeout;

public class MainActivity extends BaseActivity
        implements NavigationView.OnNavigationItemSelectedListener, ViewholderBot.OnClickListener, ActionsViewholder.OnClickListener {

    // VIEWS
    @BindView(R.id.root_container) RelativeLayout uiContainer;
    @BindView(R.id.recyclerview) RecyclerView chatRecyclerView;
    @BindView(R.id.suggestedactions) RecyclerView suggActionsRecyclerView;
    @BindView(R.id.textinputlayout) TextInputLayout textInputLayout;
    @BindView(R.id.textinput) TextInputEditText textInput;
    @BindView(R.id.drawer_layout) DrawerLayout drawer;
    @BindView(R.id.nav_view) NavigationView navigationView;
    @BindView(R.id.switch_show_textinput) SwitchCompat switchShowTextInput;
    @BindView(R.id.switch_show_full_conversation) SwitchCompat switchShowFullConversation;
    @BindView(R.id.speech_detection) TextView detectedSpeechToText;
    @BindView(R.id.agent_image) ImageView agentImage;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_main;
    private static final String LOGTAG = "MainActivity";

    // STATE
    private ChatAdapter chatAdapter;
    private ActionsAdapter suggActionsAdapter;
    private boolean alwaysShowTextInput;
    private boolean showFullConversation;
    private Handler handler;
    private boolean launchedAsAssistant;
    private Gson gson;
    private SfxManager sfxManager;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);

        handler = new Handler(Looper.getMainLooper());
        gson = new Gson();

        setupChatRecyclerView();
        setupSuggestedActionsRecyclerView();

        // Options hidden in the nav-drawer
        alwaysShowTextInput = getBooleanSharedPref(SHARED_PREF_SHOW_TEXTINPUT);
        switchShowTextInput.setChecked(alwaysShowTextInput);
        showFullConversation = getBooleanSharedPref(SHARED_PREF_SHOW_FULL_CONVERSATION);
        switchShowFullConversation.setChecked(showFullConversation);

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

        // check if this activity was launched as an assistant
        Intent intent = getIntent();
        if (intent != null) {
            String originator = intent.getStringExtra(VoiceInteractionActivity.KEY_ORIGINATOR);
            if (originator != null && originator.equals(VoiceInteractionActivity.KEY_VALUE)) {
                launchedAsAssistant = true;//this flag can now be used, i.e. to automtically start microphone recording
            }
        }

        sfxManager = new SfxManager();
        sfxManager.initialize(this);
    }

    // Register for EventBus messages and SpeechService
    @Override
    public void onStart() {
        super.onStart();
        EventBus.getDefault().register(this);
        if (speechServiceBinder == null) {
            handler.post(this::doBindService);
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

    private void setupSuggestedActionsRecyclerView() {
        suggActionsAdapter = new ActionsAdapter();
        suggActionsRecyclerView.setAdapter(suggActionsAdapter);

        LinearLayoutManager layoutManager = new LinearLayoutManager(this, LinearLayoutManager.HORIZONTAL, false);
        suggActionsRecyclerView.setLayoutManager(layoutManager);

        final int spacing = getResources().getDimensionPixelOffset(R.dimen.list_item_spacing_small);
        suggActionsRecyclerView.addItemDecoration(new ItemOffsetDecoration(spacing));
    }

    @Override
    protected void permissionDenied(String manifestPermission) {
        if (manifestPermission.equals(Manifest.permission.RECORD_AUDIO)){
            try {
                speechServiceBinder.initializeSpeechSdk(false);
                speechServiceBinder.connectAsync();
                agentImage.setVisibility(View.GONE);//hide the assistant since voice is deactivated
                textInputLayout.setVisibility(View.VISIBLE);// show the text-input prompt
            } catch (RemoteException exception){
                Log.e(LOGTAG, exception.getMessage());
            }
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
            try {
                if (speechServiceBinder != null) speechServiceBinder.startLocationUpdates();
                initializeAndConnect();
            } catch (RemoteException exception){
                Log.e(LOGTAG, exception.getMessage());
            }
        }
    }

    @Override
    protected void serviceConnected() {
        // At this point, speechServiceBinder should not be null.
        // this code is triggered after the service is bound.
        // Binding is started in onStart(), so expect this callback to trigger after onStart()
        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED) {
            initializeAndConnect();
        }

        try {
            speechServiceBinder.startLocationUpdates();
        } catch (RemoteException e) {
            e.printStackTrace();
        }
    }

    @Override
    public boolean onNavigationItemSelected(MenuItem item) {
        int id = item.getItemId();

        try {

            switch (id) {
                case R.id.nav_menu_configuration:
                    startActivity(SettingsActivity.getNewIntent(this));
                    break;
                case R.id.nav_menu_reset_bot:
                    speechServiceBinder.resetBot();
                    break;
                case R.id.nav_menu_show_assistant_settings:
                    startActivity(new Intent(Settings.ACTION_VOICE_INPUT_SETTINGS));
                    break;
            }

        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }

        drawer.closeDrawer(GravityCompat.START);

        return true;
    }

    @OnClick(R.id.agent_image)
    public void onAssistantClick() {
        try {
            showSnackbar(uiContainer, getString(R.string.msg_listening));
            sfxManager.playEarconListening();
            speechServiceBinder.listenOnceAsync();
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }
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

    @OnCheckedChanged(R.id.switch_show_full_conversation)
    public void OnShowFullConversation(CompoundButton button, boolean checked){
        showFullConversation = checked;
        putBooleanSharedPref(SHARED_PREF_SHOW_FULL_CONVERSATION, checked);
        chatAdapter.setShowFullConversation(showFullConversation);
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
        if (msg == null || msg.length() == 0) return;

        try {
            // add the users' request to the chat
            chatAdapter.addUserRequest(msg);
            // make the chat list scroll automatically after adding a bot response
            chatRecyclerView.getLayoutManager().scrollToPosition(chatAdapter.getItemCount() - 1);

            // send request to Bot
            speechServiceBinder.sendActivityMessageAsync(msg);

            sfxManager.playEarconProcessing();

            // clear out suggested actions
            String json = speechServiceBinder.getSuggestedActions();
            List<CardAction> list = gson.fromJson(json, new TypeToken<List<CardAction>>(){}.getType());
            if (list != null && list.size() > 0){
                list = null;
                speechServiceBinder.clearSuggestedActions();
                suggActionsAdapter.clear();
            }
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }
    }

    // EventBus: the connection disconnected
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventDisconnected(Disconnected event) {
        try {
            detectedSpeechToText.setText(R.string.msg_disconnected);
            boolean havePermission = ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED;
            speechServiceBinder.initializeSpeechSdk(havePermission);
            speechServiceBinder.connectAsync();
            handler.postDelayed(() -> {
                detectedSpeechToText.setText("");
            }, 2000);
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }
    }

    // EventBus: the user spoke and the app recognized intermediate speech
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventRecognizedIntermediateResult(RecognizedIntermediateResult event) {
        detectedSpeechToText.setText(event.recognized_speech);
    }

    // EventBus: the user spoke and the app recognized the speech. Disconnect mic.
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventRecognized(Recognized event) {
        sfxManager.playEarconDoneListening();
        detectedSpeechToText.setText(event.recognized_speech);
        // in 2 seconds clear the text (at this point the bot should be giving its' response)
        handler.postDelayed(() -> detectedSpeechToText.setText(""), 2000);
    }

    // EventBus: received a response from Bot
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventActivityReceived(ActivityReceived activityReceived) throws IOException {
        if (activityReceived.botConnectorActivity != null) {
            BotConnectorActivity botConnectorActivity = activityReceived.botConnectorActivity;
            sfxManager.playEarconResults();

            switch (botConnectorActivity.getType()) {
                case "message":

                    if (botConnectorActivity.getSuggestedActions() != null && botConnectorActivity.getSuggestedActions().getActions() != null) {
                        try {
                            String json = speechServiceBinder.getSuggestedActions();
                            List<CardAction> list = gson.fromJson(json, new TypeToken<List<CardAction>>(){}.getType());
                            suggActionsAdapter.addAll(list, this, this);
                        } catch (RemoteException exception){
                            Log.e(LOGTAG, exception.getMessage());
                        }
                    }

                    chatAdapter.addBotResponse(botConnectorActivity, this, this);
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

    // EventBus: the previous request has timed-out
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventRequestTimeout(RequestTimeout event) {
        // here you can notify the user to repeat the request
        sfxManager.playEarconDisambigError();
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

    // concrete implementation of ChatViewholder.OnClickListener
    @Override
    public void adaptiveCardClick(int position, String speak) {
        CardAction cardAction = null;

        try {
            String json = speechServiceBinder.getSuggestedActions();
            List<CardAction> list = gson.fromJson(json, new TypeToken<List<CardAction>>(){}.getType());
            if (list != null && list.size() > position){
                cardAction = list.get(position);
            }
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }

        // respond to the bot with the suggestedAction[position].Value if possible
        if (cardAction != null && cardAction.getValue() != null) {
            String value = (String) cardAction.getValue();
            sendTextMessage(value);
        } else {
            sendTextMessage(speak);
        }

        sfxManager.playEarconProcessing();
    }

    // concrete implementation of ActionsViewholder.OnClickListener
    @Override
    public void suggestedActionClick(int position) {
        CardAction cardAction = null;

        try {
            String json = speechServiceBinder.getSuggestedActions();
            List<CardAction> list = gson.fromJson(json, new TypeToken<List<CardAction>>(){}.getType());
            if (list != null){
                cardAction = list.get(position);
            }
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }

        if (cardAction != null) {
            String value = (String) cardAction.getValue();
            sendTextMessage(value);
            sfxManager.playEarconProcessing();
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
