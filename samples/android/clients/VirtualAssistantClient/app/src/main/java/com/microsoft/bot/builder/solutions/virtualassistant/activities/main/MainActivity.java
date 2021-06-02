package com.microsoft.bot.builder.solutions.virtualassistant.activities.main;

import android.Manifest;
import android.app.AlertDialog;
import android.app.Dialog;
import android.app.assist.AssistContent;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.drawable.AnimationDrawable;
import android.media.AudioManager;
import android.media.MediaPlayer;
import android.net.Uri;
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
import android.support.v7.app.AppCompatDelegate;
import android.support.v7.widget.AppCompatImageView;
import android.support.v7.widget.LinearLayoutManager;
import android.support.v7.widget.RecyclerView;
import android.support.v7.widget.SwitchCompat;
import android.util.Log;
import android.view.KeyEvent;
import android.view.MenuItem;
import android.view.View;
import android.view.WindowManager;
import android.view.inputmethod.EditorInfo;
import android.widget.CompoundButton;
import android.widget.ImageView;
import android.widget.RelativeLayout;
import android.widget.TextView;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.appcenter.AppCenter;
import com.microsoft.appcenter.analytics.Analytics;
import com.microsoft.appcenter.crashes.Crashes;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.linked_account.LinkedAccountActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.actionslist.ActionsAdapter;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.actionslist.ActionsViewholder;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist.Action;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist.ChatAdapter;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist.ItemOffsetDecoration;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.settings.SettingsActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.utils.AppConfiguration;

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
import butterknife.OnTextChanged;
import client.model.BotConnectorActivity;
import client.model.CardAction;
import events.ActivityReceived;
import events.BotListening;
import events.Connected;
import events.Disconnected;
import events.Recognized;
import events.RecognizedIntermediateResult;
import events.RequestTimeout;
import io.adaptivecards.objectmodel.ActionType;
import io.adaptivecards.objectmodel.BaseActionElement;
import io.adaptivecards.objectmodel.BaseCardElement;
import io.adaptivecards.renderer.RenderedAdaptiveCard;
import io.adaptivecards.renderer.actionhandler.ICardActionHandler;

public class MainActivity extends BaseActivity
        implements NavigationView.OnNavigationItemSelectedListener, ICardActionHandler, ActionsViewholder.OnClickListener {

    // VIEWS
    @BindView(R.id.root_container) RelativeLayout uiContainer;
    @BindView(R.id.recyclerview) RecyclerView chatRecyclerView;
    @BindView(R.id.suggestedactions) RecyclerView suggActionsRecyclerView;
    @BindView(R.id.textinputlayout) TextInputLayout textInputLayout;
    @BindView(R.id.textinput) TextInputEditText textInput;
    @BindView(R.id.drawer_layout) DrawerLayout drawer;
    @BindView(R.id.nav_view) NavigationView navigationView;
    @BindView(R.id.speech_detection) TextView detectedSpeechToText;
    @BindView(R.id.mic_image) ImageView micImage;
    @BindView(R.id.kbd_image) ImageView kbdImage;
    @BindView(R.id.animated_assistant) AppCompatImageView animatedAssistant;
    @BindView(R.id.switch_enable_kws) SwitchCompat switchEnableKws;
    @BindView(R.id.switch_enable_barge_in) SwitchCompat switchEnableBargeIn;
    @BindView(R.id.nav_menu_set_as_default_assistant) TextView setDefaultAssistant;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_main;
    private static final String LOGTAG = "MainActivity";
    private static final int REQUEST_CODE_SETTINGS = 256;
    private static final int REQUEST_CODE_OVERLAY_PERMISSION = 255;
    private static final String PARAMS_USER_ID = "userId";
    private static final String PARAMS_SIGN_IN_STATUS = "signInStatus";

    // STATE
    private ChatAdapter chatAdapter;
    private ActionsAdapter suggActionsAdapter;
    private Handler handler;
    private Gson gson;
    private SfxManager sfxManager;
    private boolean enableDarkMode;
    private boolean keepScreenOn;
    private boolean enableKws;
    private boolean bargeInSupported;
    private boolean isExpandedTextInput;
    private boolean isCreated;// used to identify when onCreate() is complete, used with SwitchCompat

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
        Configuration speechConfig = configurationManager.getConfiguration();
        enableKws = speechConfig.enableKWS;
        bargeInSupported = speechConfig.ttsBargeInSupported;
        switchEnableKws.setChecked(enableKws);
        switchEnableBargeIn.setChecked(bargeInSupported);

        // NAV DRAWER
        ActionBarDrawerToggle toggle = new ActionBarDrawerToggle( this, drawer, R.string.navigation_drawer_open, R.string.navigation_drawer_close);
        drawer.addDrawerListener(toggle);
        toggle.syncState();
        navigationView.setNavigationItemSelectedListener(this);
        setSignInStatus(configurationManager.getConfiguration().signedIn);
        if (configurationManager.getConfiguration().linkedAccountEndpoint == null || configurationManager.getConfiguration().linkedAccountEndpoint.isEmpty()) {
            navigationView.getMenu().findItem(R.id.nav_menu_sign_in).setVisible(false);
        }

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

        sfxManager = new SfxManager();
        sfxManager.initialize(this);

        // assign animation
        animatedAssistant.setBackgroundResource(R.drawable.agent_listening_animation);

        // load configurations from shared preferences
        loadAppConfiguration();

        AppCenter.start(getApplication(), appConfigurationManager.getConfiguration().appCenterId,
                Analytics.class, Crashes.class);

        Analytics.setEnabled(true);
        Crashes.setEnabled(true);

        isCreated = true;//keep this as last line in onCreate()
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
        if (keepScreenOn) {
            getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON); // to keep screen on
        }
        Intent intent = getIntent();
        Uri intentData = intent.getData();
        if (intent != null && intentData != null) {
            String userId = intentData.getQueryParameter(PARAMS_USER_ID);
            Boolean signInStatus = Boolean.parseBoolean(intentData.getQueryParameter(PARAMS_SIGN_IN_STATUS).toLowerCase());

            Configuration configuration = configurationManager.getConfiguration();
            configuration.userId = userId;
            configuration.signedIn = signInStatus;
            configurationManager.setConfiguration(configuration);
            setSignInStatus(signInStatus);
        }
    }

    @Override
    protected void onPause() {
        super.onPause();
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON); // to disable keeping screen on
    }

    // Unregister EventBus messages and SpeechService
    @Override
    public void onStop() {
        super.onStop();
        EventBus.getDefault().unregister(this);
        if (myConnection != null) {
            unbindService(myConnection);
            speechServiceBinder = null;
        }
    }

    private void setupChatRecyclerView() {
        chatAdapter = new ChatAdapter(this);
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

    private void setSignInStatus(boolean signInStatus) {
        navigationView.getMenu().findItem(R.id.nav_menu_sign_in).setTitle(signInStatus ? R.string.nav_menu_sign_out : R.string.nav_menu_sign_in);
    }

    @Override
    protected void permissionDenied(String manifestPermission) {
        if (manifestPermission.equals(Manifest.permission.RECORD_AUDIO)){
            try {
                speechServiceBinder.initializeSpeechSdk(false);
                speechServiceBinder.connectAsync();
                micImage.setVisibility(View.GONE);//hide the mic since voice is deactivated
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
//            initializeAndConnect();
            if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
                requestFineLocationPermissions();
            }
        }
        if (manifestPermission.equals(Manifest.permission.ACCESS_FINE_LOCATION)){
            try {
                if (speechServiceBinder != null) speechServiceBinder.startLocationUpdates();
                //initializeAndConnect();
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
//            initializeAndConnect();
            boolean enabled = setKwsState(enableKws);
            if (!enabled && enableKws) {
                switchEnableKws.setChecked(false);
            }
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
                case R.id.nav_menu_settings:
                    startActivityForResult(SettingsActivity.getNewIntent(this), REQUEST_CODE_SETTINGS);
                    break;
                case R.id.nav_menu_restart_conversation:
                    chatAdapter.resetChat();
                    suggActionsAdapter.clear();
                    speechServiceBinder.clearSuggestedActions();
                    resetSpeechService();
                    break;
                case R.id.nav_menu_sign_in:
                    startActivity(LinkedAccountActivity.getNewIntent(this));
            }

        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }

        drawer.closeDrawer(GravityCompat.START);

        return true;
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == REQUEST_CODE_SETTINGS && resultCode == RESULT_OK) {
            loadAppConfiguration();
        } else if (requestCode == REQUEST_CODE_OVERLAY_PERMISSION) {
            if (Settings.canDrawOverlays(this)) {
                startActivity(new Intent(Settings.ACTION_VOICE_INPUT_SETTINGS));
            }
        }
    }

    private void loadAppConfiguration() {
        AppConfiguration appConfiguration = appConfigurationManager.getConfiguration();
        setDarkMode(appConfiguration.enableDarkMode);
        keepScreenOn = appConfiguration.keepScreenOn;
        chatAdapter.setShowFullConversation(appConfiguration.showFullConversation);
        chatAdapter.setChatItemHistoryCount(appConfiguration.historyLinecount);
        chatAdapter.setChatBubbleColors(appConfiguration.colorBubbleBot, appConfiguration.colorBubbleUser);
        chatAdapter.setChatTextColors(appConfiguration.colorTextBot, appConfiguration.colorTextUser);
    }

    private void showListeningAnimation(){
        Log.i(LOGTAG, "Listening again - showListeningAnimation()");
        animatedAssistant.setVisibility(View.VISIBLE);
        ((AnimationDrawable) animatedAssistant.getBackground()).start();
        sfxManager.playEarconListening();
    }

    private void hideListeningAnimation(){
        Log.i(LOGTAG, "Listening again - hideListeningAnimation()");
        animatedAssistant.setVisibility(View.GONE);
        sfxManager.playEarconDoneListening();
    }

    private void resetSpeechService() {
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

    @OnClick(R.id.kbd_image)
    public void onClickKeyboard() {
        if (isExpandedTextInput)
            textInputLayout.setVisibility(View.GONE);
        else
            textInputLayout.setVisibility(View.VISIBLE);
        isExpandedTextInput = !isExpandedTextInput;
    }

    @OnClick(R.id.mic_image)
    public void onClickAssistant() {
        try {
            speechServiceBinder.stopAnyTTS();
            //showListeningAnimation();
            speechServiceBinder.listenOnceAsync();
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }
    }

    @OnClick(R.id.nav_menu_set_as_default_assistant)
    public void onClickSetDefaultAssistant() {
        if (Settings.canDrawOverlays(this)) {
            startActivity(new Intent(Settings.ACTION_VOICE_INPUT_SETTINGS));
        } else {
            Intent intent = new Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION);
            intent.setData(Uri.parse("package:" + getPackageName()));
            startActivityForResult(intent, REQUEST_CODE_OVERLAY_PERMISSION);
        }
    }

    @OnCheckedChanged(R.id.switch_enable_kws)
    public void onCheckedChangedEnableKws(CompoundButton button, boolean checked){
        if (isCreated) {
            if (speechServiceBinder != null) {
                // if there's a connection to the service, go ahead and toggle Kws
                enableKws = setKwsState(checked);//returns true only if Kws is turned on
            } else {
                // defer toggling Kws for later, for now records users' wishes
                enableKws = checked;
            }

            Configuration speechConfig =  configurationManager.getConfiguration();
            speechConfig.enableKWS = enableKws;
            configurationManager.setConfiguration(speechConfig);

            if (checked && !enableKws) {
                switchEnableKws.setChecked(false);
            }
        }
    }

    @OnCheckedChanged(R.id.switch_enable_barge_in)
    public void onCheckedChangedEnableBargeIn(CompoundButton button, boolean checked) {
        if (isCreated) {
            bargeInSupported = checked;

            Configuration speechConfig = configurationManager.getConfiguration();
            speechConfig.ttsBargeInSupported = bargeInSupported;
            configurationManager.setConfiguration(speechConfig);
        }
    }

    public void setDarkMode(boolean enabled){
        if (enableDarkMode != enabled) {
            enableDarkMode = enabled;

            // OutOfMemoryError can occur, try to free as many objects as possible 1st
            // note: the assistant animation might need to be unloaded prior to switching night mode
            sfxManager.reset();
            sfxManager = null;
            System.gc();

            // now proceed with the night mode switch
            AppCompatDelegate.setDefaultNightMode(enabled ? AppCompatDelegate.MODE_NIGHT_YES : AppCompatDelegate.MODE_NIGHT_NO);
            getDelegate().applyDayNight();

            // re-init SFX manager
            sfxManager = new SfxManager();
            sfxManager.initialize(this);
        }
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

    @OnTextChanged(R.id.textinput)
    protected void onTextChanged(CharSequence text) {
        try {
            if (speechServiceBinder != null) speechServiceBinder.stopAnyTTS();
        } catch (RemoteException e) {
            e.printStackTrace();
        }
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
            chatAdapter.resetChat();
            suggActionsAdapter.clear();
            speechServiceBinder.clearSuggestedActions();
            speechServiceBinder.disconnectAsync();
        } catch (RemoteException exception) {
            Log.e(LOGTAG, exception.getMessage());
        }

        String cancelErrorFormat =
                "CANCELED: Reason = %d\n" +
                "CANCELED: ErrorCode = %d\n" +
                "CANCELED: ErrorDetails = %s\n" +
                "CANCELED: Did you update the subscription key and region?";
        String cancelErrorMessage = String.format(cancelErrorFormat, event.cancellationReason, event.errorCode, event.errorDetails);

        new AlertDialog.Builder(this)
                .setTitle(R.string.msg_canceled)
                .setMessage(cancelErrorMessage)
                .setPositiveButton(R.string.settings, new DialogInterface.OnClickListener() {
                    @Override
                    public void onClick(DialogInterface dialogInterface, int i) {
                        // Navigate user to settings to update speech key/region
                        Context context = ((Dialog) dialogInterface).getContext();
                        startActivityForResult(SettingsActivity.getNewIntent(context), REQUEST_CODE_SETTINGS);
                    }
                })
                .setNegativeButton(R.string.cancel,null)
                .show();
    }

    // EventBus: the Bot is listening
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventBotListening(BotListening event) {

        // Note: the SpeechService will trigger the actual listening. Since the app needs to show a
        // visual, the app needs to subscribe to this event and act on it.
        showListeningAnimation();

        if ((bargeInSupported)) {
            try {
                speechServiceBinder.stopAnyTTS();
            } catch (RemoteException exception) {
                Log.e(LOGTAG, exception.getMessage());
            }
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
        hideListeningAnimation();
        if (event.recognized_speech.length()>0) {
            detectedSpeechToText.setText(event.recognized_speech);
            chatAdapter.addUserRequest(event.recognized_speech);

            // in 2 seconds clear the text (at this point the bot should be giving its' response)
            handler.postDelayed(() -> detectedSpeechToText.setText(""), 2000);
        }
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

                    chatAdapter.addBotResponse(botConnectorActivity);
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

    // adaptive card action handlers
    @Override
    public void onAction(BaseActionElement baseActionElement, RenderedAdaptiveCard renderedAdaptiveCard) {
        ActionType actionType = baseActionElement.GetElementType();
        if (actionType == ActionType.Submit) { // only Action.Submit supported for now
            // cannot get "data" field from action directly, so we need to serialize it first
            String jsonString = baseActionElement.Serialize();
            Action action = new Gson().fromJson(jsonString, Action.class);
            if (action != null && action.data != null) {
                try {
                    speechServiceBinder.stopAnyTTS();
                } catch (RemoteException e) {
                    e.printStackTrace();
                }
                sendTextMessage(action.data);
                sfxManager.playEarconProcessing();
            }
        }
    }

    // required method of ICardActionHandler, not implemented yet
    @Override
    public void onMediaPlay(BaseCardElement baseCardElement, RenderedAdaptiveCard renderedAdaptiveCard) {
    }

    // required method of ICardActionHandler, not implemented yet
    @Override
    public void onMediaStop(BaseCardElement baseCardElement, RenderedAdaptiveCard renderedAdaptiveCard) {
    }
}
