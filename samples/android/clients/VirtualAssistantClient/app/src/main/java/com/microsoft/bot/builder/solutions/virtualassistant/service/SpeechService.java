package com.microsoft.bot.builder.solutions.virtualassistant.service;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.appwidget.AppWidgetManager;
import android.content.ActivityNotFoundException;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.res.AssetManager;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.graphics.PixelFormat;
import android.graphics.drawable.AnimationDrawable;
import android.location.Location;
import android.media.MediaPlayer;
import android.net.Uri;
import android.os.Build;
import android.os.IBinder;
import android.os.RemoteException;
import android.provider.Settings;
import android.support.v4.app.NotificationCompat;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.WindowManager;
import android.widget.RemoteViews;
import android.widget.Toast;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.appcenter.analytics.Analytics;
import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.SpeechSdk;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.ISpeechService;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.main.SfxManager;
import com.microsoft.bot.builder.solutions.virtualassistant.models.OpenDefaultApp;
import com.microsoft.bot.builder.solutions.virtualassistant.utils.PlayStoreUtils;
import com.microsoft.bot.builder.solutions.virtualassistant.widgets.WidgetBotRequest;
import com.microsoft.bot.builder.solutions.virtualassistant.widgets.WidgetBotResponse;

import org.greenrobot.eventbus.EventBus;
import org.greenrobot.eventbus.Subscribe;
import org.greenrobot.eventbus.ThreadMode;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.util.Map;

import client.model.ActivityValue;
import client.model.BotConnectorActivity;
import client.model.InputHints;
import events.ActivityReceived;
import events.Recognized;
import events.RecognizedIntermediateResult;
import events.RequestTimeout;
import events.SynthesizerStopped;

/**
 * The SpeechService is the connection between bot and activities and widgets
 * The SpeechService should always be running in the background, ready to interact with the widgets
 * and always ready to process data immediately.
 * Running this service as a ForegroundService best suits this requirement.
 *
 * NOTE: the service assumes that it has/will have permission to RECORD_AUDIO
 */
public class SpeechService extends Service {

    // CONSTANTS
    private static final String TAG_FOREGROUND_SERVICE = "SpeechService";
    public static final String ACTION_START_FOREGROUND_SERVICE = "ACTION_START_FOREGROUND_SERVICE";
    public static final String ACTION_STOP_FOREGROUND_SERVICE = "ACTION_STOP_FOREGROUND_SERVICE";
    public static final String ACTION_START_LISTENING = "ACTION_START_LISTENING";

    // STATE
    private ISpeechService.Stub binder;
    private SpeechSdk speechSdk;
    private ConfigurationManager configurationManager;
    private LocationProvider locationProvider;
    private Gson gson;
    private boolean shouldListenAgain;
    private boolean previousRequestWasTyped;
    private View animationView;
    private SfxManager sfxManager;

    // CONSTRUCTOR
    public SpeechService() {
        binder = new ISpeechService.Stub() {
            @Override
            public boolean isSpeechSdkRunning() {
                return speechSdk != null;
            }

            @Override
            public void sendTextMessage(String msg) {
                if (speechSdk != null) speechSdk.sendActivityMessageAsync(msg);
            }

            /**
             * Initialize the speech SDK.
             * Note: This can be called repeatedly without negative consequences, i.e. device rotation
             * @param haveRecordAudioPermission true or false
             */
            @Override
            public void initializeSpeechSdk(boolean haveRecordAudioPermission){
                SpeechService.this.initializeSpeechSdk(haveRecordAudioPermission);
            }

            @Override
            public void connectAsync(){
                if (speechSdk != null) speechSdk.connectAsync();
            }

            @Override
            public void disconnectAsync() {
                if (speechSdk != null) speechSdk.disconnectAsync();
            }

            @Override
            public void startLocationUpdates() {
                SpeechService.this.startLocationUpdates();
            }

            @Override
            public String getConfiguration(){
                return gson.toJson(configurationManager.getConfiguration());
            }

            @Override
            public void sendLocationEvent(String lat, String lon){
                if (speechSdk != null) speechSdk.sendLocationEvent(lat, lon);
            }

            @Override
            public void requestWelcomeCard(){
                if (speechSdk != null) speechSdk.requestWelcomeCard();
            }

            @Override
            public void injectReceivedActivity(String json){
                if (speechSdk != null) speechSdk.activityReceived(json);
            }

            @Override
            public void listenOnceAsync(){
                if (speechSdk != null) {
                    speechSdk.listenOnceAsync();
                    previousRequestWasTyped = false;
                }
            }

            @Override
            public void sendActivityMessageAsync(String msg){
                if (speechSdk != null) {
                    speechSdk.sendActivityMessageAsync(msg);
                    Analytics.trackEvent("Activity sent");
                    previousRequestWasTyped = true;
                }
            }

            @Override
            public String getSuggestedActions(){
                String suggestedActions = null;
                if (speechSdk != null) {
                    suggestedActions = gson.toJson(speechSdk.getSuggestedActions());
                }
                return suggestedActions;
            }

            @Override
            public void clearSuggestedActions(){
                if (speechSdk != null) speechSdk.clearSuggestedActions();
            }

            @Override
            public void startKeywordListeningAsync(String keyword) {
                if (speechSdk != null) {
                    AssetManager am = getAssets();
                    try {
                        InputStream is = am.open("keywords/" + keyword + "/kws.table");
                        speechSdk.startKeywordListeningAsync(is, keyword);
                    } catch (Exception e) {
                        e.printStackTrace();
                    }
                }
            }

            @Override
            public void stopKeywordListening() {
                if (speechSdk != null) speechSdk.stopKeywordListening();
            }

            @Override
            public void setConfiguration(String json) {
                Configuration configuration = gson.fromJson(json, new TypeToken<Configuration>(){}.getType());
                configurationManager.setConfiguration(configuration);
            }

            @Override
            public void stopAnyTTS() {
                if(speechSdk != null && speechSdk.getSynthesizer().isPlaying()){
                    speechSdk.getSynthesizer().stopSound();
                }
            }

            @Override
            public String getDateSentLocationEvent() {
                if(speechSdk != null) return speechSdk.getDateSentLocationEvent();
                return "Error";
            }

            @Override
            public void sendLocationUpdate() {
                Location location = locationProvider.getLastKnownLocation();
                if (location != null) {
                    final String locLat = String.valueOf(location.getLatitude());
                    final String locLon = String.valueOf(location.getLongitude());
                    if (speechSdk != null) {
                        speechSdk.sendLocationEvent(locLat, locLon);
                        Analytics.trackEvent("VA.Location event sent");
                    }
                } else {
                    Toast.makeText(getApplicationContext(), "Location is unknown", Toast.LENGTH_LONG).show();
                }
            }
        };
    }

    @Override
    public IBinder onBind(Intent intent) {
        Log.d(TAG_FOREGROUND_SERVICE, "onBind()");
        return binder;
    }

    @Override
    public boolean onUnbind(Intent intent) {
        Log.d(TAG_FOREGROUND_SERVICE, "onUnbind()");
        return super.onUnbind(intent);
    }

    @Override
    public void onCreate() {
        super.onCreate();
        Log.d(TAG_FOREGROUND_SERVICE, "onCreate()");
        EventBus.getDefault().register(this);
        gson = new Gson();

        configurationManager = new ConfigurationManager(this);

        locationProvider = new LocationProvider(this, location -> {
            final String locLat = String.valueOf(location.getLatitude());
            final String locLon = String.valueOf(location.getLongitude());
            if (speechSdk != null) {
                speechSdk.sendLocationEvent(locLat, locLon);
            }
        });

        // Initialize SFX manager
        sfxManager = new SfxManager();
        sfxManager.initialize(this);
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        EventBus.getDefault().unregister(this);
        stopListening();
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if(intent != null) {
            String action = intent.getAction();
            if (action == null) action = ACTION_START_FOREGROUND_SERVICE;

            switch (action) {
                case ACTION_START_FOREGROUND_SERVICE:
                    Toast.makeText(getApplicationContext(), "Service is started", Toast.LENGTH_LONG).show();
                    if (speechSdk == null) initializeSpeechSdk(true);//assume true - for this to work the app must have been launched once for permission dialog
                    startForegroundService();
                    break;
                case ACTION_STOP_FOREGROUND_SERVICE:
                    Toast.makeText(getApplicationContext(), "Service is stopped", Toast.LENGTH_LONG).show();
                    stopForegroundService();
                    break;
                case ACTION_START_LISTENING:
                    startListening();
                    break;
            }
        }
        return START_STICKY;
    }

    private void startForegroundService() {
        Log.d(TAG_FOREGROUND_SERVICE, "startForegroundService() starting");

        // Create notification default intent
        Intent intent = new Intent();
        PendingIntent pendingIntent = PendingIntent.getActivity(this, 0, intent, 0);

        // Create Notification Channel
        NotificationManager notificationManager = (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);

        String channelId = "some_channel_id";

        // Starting with API level 26
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            CharSequence channelName = "Some Channel";
            int importance = NotificationManager.IMPORTANCE_LOW;
            NotificationChannel notificationChannel = new NotificationChannel(channelId, channelName, importance);
            notificationChannel.enableLights(true);
            notificationChannel.setLightColor(Color.RED);
            notificationChannel.enableVibration(true);
            notificationChannel.setVibrationPattern(new long[]{100, 200, 300, 400, 500, 400, 300, 200, 100});
            notificationManager.createNotificationChannel(notificationChannel);
        }

        // Create notification builder
        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, channelId);

        // Make notification show big text
        NotificationCompat.BigTextStyle bigTextStyle = new NotificationCompat.BigTextStyle();
        bigTextStyle.setBigContentTitle("Speech recognitions service");
        bigTextStyle.bigText("Speech Service is active. Click LISTEN to speak to the Bot");

        // Set big text style.
        builder.setStyle(bigTextStyle);

        // Timestamp
        builder.setWhen(System.currentTimeMillis());

        // Icon
        builder.setSmallIcon(R.drawable.ic_va);
        Bitmap largeIconBitmap = BitmapFactory.decodeResource(getResources(), R.drawable.ic_va);
        builder.setLargeIcon(largeIconBitmap);

        // Set notification priority
        builder.setPriority(NotificationManager.IMPORTANCE_LOW);

        // Add LISTEN button intent in notification
        Intent listenIntent = new Intent(this, SpeechService.class);
        listenIntent.setAction(ACTION_START_LISTENING);
        PendingIntent pendingPlayIntent = PendingIntent.getService(this, 0, listenIntent, 0);
        NotificationCompat.Action playAction = new NotificationCompat.Action(R.drawable.ic_mic, getString(R.string.service_notification_action_button), pendingPlayIntent);
        builder.addAction(playAction);

        // Build the notification
        Notification notification = builder.build();

        // Start foreground service
        startForeground(STOP_FOREGROUND_REMOVE, notification);

        Log.d(TAG_FOREGROUND_SERVICE, "startForegroundService() complete");
    }

    private void stopForegroundService() {
        Log.d(TAG_FOREGROUND_SERVICE, "stopForegroundService()");

        locationProvider.stopLocationUpdates();

        // Stop foreground service and remove the notification
        stopForeground(true);

        // Stop the foreground service
        stopSelf();
    }

    public void startLocationUpdates() {
        locationProvider.startLocationUpdates();
    }

    private void initializeSpeechSdk(boolean haveRecordAudioPermission){
        if (speechSdk != null) {
            Log.d(TAG_FOREGROUND_SERVICE, "resetting SpeechSDK");
            shouldListenAgain = false;
            previousRequestWasTyped = false;
            speechSdk.disconnectAsync();
        }
        speechSdk = new SpeechSdk();
        File directory = getExternalFilesDir(null);
        Configuration configuration = configurationManager.getConfiguration();
        speechSdk.initialize(configuration, haveRecordAudioPermission, directory.getPath());
        if (configuration.enableKWS) {
            try {
                binder.startKeywordListeningAsync(configuration.keyword);
            } catch (RemoteException e) {
                e.printStackTrace();
            }
        }
    }

    // Initialize listening animation view
    private void initializeAnimation() {
        if (Settings.canDrawOverlays(this)) {
            LayoutInflater inflater = (LayoutInflater) getSystemService(LAYOUT_INFLATER_SERVICE);
            WindowManager.LayoutParams params = new WindowManager.LayoutParams(
                    WindowManager.LayoutParams.WRAP_CONTENT,
                    WindowManager.LayoutParams.WRAP_CONTENT,
                    Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O ? WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY : WindowManager.LayoutParams.TYPE_SYSTEM_ALERT,
                    WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE
                            | WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL
                            | WindowManager.LayoutParams.FLAG_WATCH_OUTSIDE_TOUCH,
                    PixelFormat.TRANSLUCENT);

            WindowManager wm = (WindowManager) getSystemService(WINDOW_SERVICE);
            View animationContainer = inflater.inflate(R.layout.assistant_overlay, null);
            wm.addView(animationContainer, params);
            animationView = animationContainer.findViewById(R.id.animated_assistant);
        }
    }

    // EventBus: the synthesizer has stopped playing
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventSynthesizerStopped(SynthesizerStopped event) {
        if (previousRequestWasTyped){
            previousRequestWasTyped = false;
            shouldListenAgain = false;
        }

        if(shouldListenAgain){
            shouldListenAgain = false;
            Log.i(TAG_FOREGROUND_SERVICE, "Listening again");
            speechSdk.listenOnceAsync();
        }

    }

    // EventBus: the previous request timed out
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventRequestTimeout(RequestTimeout event) {
        broadcastTimeout(event);
        stopListening();
    }

    // EventBus: the user spoke and the app recognized intermediate speech
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventRecognizedIntermediateResult(RecognizedIntermediateResult event) {
        updateBotRequestWidget(event.recognized_speech);
    }

    // EventBus: the user spoke and the app recognized the speech. Disconnect mic.
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventRecognized(Recognized event) {
        updateBotRequestWidget(event.recognized_speech);
        stopListening();
    }

    // EventBus: received a response from Bot
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventActivityReceived(ActivityReceived activityReceived) throws IOException {
        if (activityReceived.botConnectorActivity != null) {
            BotConnectorActivity botConnectorActivity = activityReceived.botConnectorActivity;
            Analytics.trackEvent("Activity received");

            switch (botConnectorActivity.getType()) {
                case "message":
                    // update Response widget
                    updateBotResponseWidget(botConnectorActivity.getText());
                    // update client apps
                    broadcastActivity(botConnectorActivity);
                    break;
                case "dialogState":
                    Log.i(TAG_FOREGROUND_SERVICE, "Activity with DialogState");
                    break;
                case "PlayLocalFile":
                    Log.i(TAG_FOREGROUND_SERVICE, "Activity with PlayLocalFile");
                    playMediaStream(botConnectorActivity.getFile());
                    break;
                case "event":
                    if (botConnectorActivity.getName().equals("OpenDefaultApp")) {
                        Log.i(TAG_FOREGROUND_SERVICE, "OpenDefaultApp");
                        openDefaultApp(botConnectorActivity);
                    } else {
                        // all other events are broadcast for other apps
                        broadcastWidgetUpdate(botConnectorActivity);
                    }
                    break;
                default:
                    // all other events are broadcast for other apps
                    broadcastWidgetUpdate(botConnectorActivity);
                    break;
            }

            // make the bot automatically listen again
            if(botConnectorActivity.getInputHint() != null){
                Log.i(TAG_FOREGROUND_SERVICE, "InputHint: "+botConnectorActivity.getInputHint());
                if(botConnectorActivity.getInputHint().equals(InputHints.EXPECTINGINPUT.toString())){
                    shouldListenAgain = true;
                }
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
            Log.e(TAG_FOREGROUND_SERVICE, "IOexception " + e.getMessage());
        }
    }

    private void openDefaultApp(BotConnectorActivity botConnectorActivity) {
        String intentStr = null;
        if (botConnectorActivity.getValue() instanceof String) {
            intentStr = (String) botConnectorActivity.getValue();
        } else if (botConnectorActivity.getValue() instanceof Map) {
            intentStr = gson.toJson(botConnectorActivity.getValue());
        } else {
            intentStr = ((ActivityValue) botConnectorActivity.getValue()).getUri();
        }
        if (intentStr != null) {
            OpenDefaultApp event = gson.fromJson(intentStr, OpenDefaultApp.class);
            if (event.mapsUri != null && !event.mapsUri.isEmpty()) {
                final String gpscoords = event.mapsUri.replace("geo:", "");

                try {
                    // Launch Waze
                    Uri wazeIntentUri = Uri.parse("waze://?ll=" + gpscoords + "&navigate=yes");
                    Intent wazeIntent = new Intent(Intent.ACTION_VIEW, wazeIntentUri);
                    wazeIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                    startActivity(wazeIntent);
                } catch (ActivityNotFoundException ex) {
                    // launch Google Maps
                    Uri gmmIntentUri = Uri.parse("google.navigation:q=" + gpscoords);//NOTE: by default mode = driving
                    Intent mapIntent = new Intent(Intent.ACTION_VIEW, gmmIntentUri);
                    mapIntent.setPackage("com.google.android.apps.maps");//NOTE: this will exclusively use Google maps. TODO allow Waze too
                    mapIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                    if (mapIntent.resolveActivity(getPackageManager()) != null) {
                        startActivity(mapIntent);
                    } else {
                        // since Waze and Google maps aren't available, show error to user
                        PlayStoreUtils.launchPlayStore(this, "com.google.android.apps.maps");
                        Toast.makeText(this, R.string.service_error_no_map, Toast.LENGTH_LONG).show();
                    }
                }

            }

            if (event.meetingUri != null && !event.meetingUri.isEmpty()) {
                Uri intentUri = Uri.parse(event.meetingUri);
                Intent meetingUrlIntent = new Intent(Intent.ACTION_VIEW);
                meetingUrlIntent.setData(intentUri);
                if (meetingUrlIntent.resolveActivity(getPackageManager()) != null) {
                    startActivity(meetingUrlIntent);
                }
            }
            else if (event.telephoneUri != null && !event.telephoneUri.isEmpty()) {
                Uri intentUri = Uri.parse(event.telephoneUri);
                Intent dialerIntent = new Intent(Intent.ACTION_DIAL, intentUri);
                dialerIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                if (dialerIntent.resolveActivity(getPackageManager()) != null) {
                    startActivity(dialerIntent);
                }
            }

            if (event.musicUri != null && !event.musicUri.isEmpty()) {
                try {
                    // please note that ":play" makes Spotify automatically start playing
                    Intent spotifyIntent = new Intent(Intent.ACTION_VIEW, Uri.parse(event.musicUri + ":play"));
                    spotifyIntent.setPackage("com.spotify.music");
                    spotifyIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                    startActivity(spotifyIntent);
                } catch (ActivityNotFoundException ex) {
                    PlayStoreUtils.launchPlayStore(this,"com.spotify.music");
                    Toast.makeText(this, R.string.service_error_no_spotify, Toast.LENGTH_LONG).show();
                }
            }
        } else {
            Toast.makeText(this, R.string.service_error_uri, Toast.LENGTH_LONG).show();
        }
    }

    private void broadcastActivity(BotConnectorActivity botConnectorActivity){
        final String json = gson.toJson(botConnectorActivity);
        final Intent intent=new Intent();
        intent.setAction("com.microsoft.broadcast");
        intent.putExtra("BotConnectorActivity",json);
        sendBroadcast(intent);
    }

    private void broadcastTimeout(RequestTimeout event){
        final String json = gson.toJson(event);
        final Intent intent=new Intent();
        intent.setAction("com.microsoft.broadcast");
        intent.putExtra("RequestTimeout",json);
        sendBroadcast(intent);
    }

    private void broadcastWidgetUpdate(BotConnectorActivity botConnectorActivity){
        final String json = gson.toJson(botConnectorActivity);
        final Intent intent=new Intent();
        intent.setAction("com.microsoft.broadcast");
        intent.putExtra("WidgetUpdate",json);
        sendBroadcast(intent);
    }

    private void updateBotResponseWidget(String text){
        Log.v(TAG_FOREGROUND_SERVICE, "updateBotResponseWidget("+text+")");
        Context context = this;
        AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
        RemoteViews remoteViews = new RemoteViews(context.getPackageName(), R.layout.widget_bot_response);
        ComponentName thisWidget = new ComponentName(context, WidgetBotResponse.class);
        remoteViews.setTextViewText(R.id.appwidget_text, text);
        appWidgetManager.updateAppWidget(thisWidget, remoteViews);
    }

    private void updateBotRequestWidget(String text){
        Log.v(TAG_FOREGROUND_SERVICE, "updateBotRequestWidget("+text+")");
        Context context = this;
        AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
        RemoteViews remoteViews = new RemoteViews(context.getPackageName(), R.layout.widget_bot_request);
        ComponentName thisWidget = new ComponentName(context, WidgetBotRequest.class);
        remoteViews.setTextViewText(R.id.appwidget_text, text);
        appWidgetManager.updateAppWidget(thisWidget, remoteViews);
    }

    private void startListening() {
        Toast.makeText(getApplicationContext(), "Listening", Toast.LENGTH_LONG).show();
        if (speechSdk == null) {
            initializeSpeechSdk(true); // assume true - for this to work the app must have been launched once for permission dialog
        }
        speechSdk.connectAsync();
        speechSdk.listenOnceAsync();
        if (animationView == null) {
            initializeAnimation(); // initialize listening animation view
        }
        if (animationView != null) {
            animationView.setVisibility(View.VISIBLE);
            ((AnimationDrawable)animationView.getBackground()).start();
        }
        if (sfxManager != null) {
            sfxManager.playEarconListening();
        }
    }

    private void stopListening() {
        if (animationView != null) {
            ((AnimationDrawable)animationView.getBackground()).stop();
            animationView.setVisibility(View.GONE);
        }
        if (sfxManager != null) {
            sfxManager.playEarconDoneListening();
        }
    }
}
