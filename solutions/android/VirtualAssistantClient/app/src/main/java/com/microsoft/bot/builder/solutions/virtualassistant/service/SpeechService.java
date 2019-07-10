package com.microsoft.bot.builder.solutions.virtualassistant.service;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.appwidget.AppWidgetManager;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.media.MediaPlayer;
import android.net.Uri;
import android.os.Build;
import android.os.IBinder;
import android.os.RemoteException;
import android.support.v4.app.NotificationCompat;
import android.util.Log;
import android.widget.RemoteViews;
import android.widget.Toast;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.SpeechSdk;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.ISpeechService;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.configuration.DefaultConfiguration;
import com.microsoft.bot.builder.solutions.virtualassistant.widgets.WidgetBotRequest;
import com.microsoft.bot.builder.solutions.virtualassistant.widgets.WidgetBotResponse;

import org.greenrobot.eventbus.EventBus;
import org.greenrobot.eventbus.Subscribe;
import org.greenrobot.eventbus.ThreadMode;

import java.io.File;
import java.io.IOException;

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
 * NOTE: the service assumes that it has permission to RECORD_AUDIO
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

    // CONSTRUCTOR
    public SpeechService() {
        binder = new ISpeechService.Stub() {
            @Override
            public boolean isSpeechSdkRunning() throws RemoteException {
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
                speechSdk.connectAsync();
            }

            @Override
            public void startLocationUpdates() {
                SpeechService.this.startLocationUpdates();
            }

            @Override
            public void resetBot(){
                shouldListenAgain = false;
                speechSdk.resetBot(configurationManager.getConfiguration());
            }

            @Override
            public String getConfiguration(){
                return gson.toJson(configurationManager.getConfiguration());
            }

            @Override
            public void sendLocationEvent(String lat, String lon){
                speechSdk.sendLocationEvent(lat, lon);
            }

            @Override
            public void requestWelcomeCard(){
                speechSdk.requestWelcomeCard();
            }

            @Override
            public void injectReceivedActivity(String json){
                speechSdk.activityReceived(json);
            }

            @Override
            public void listenOnceAsync(){
                speechSdk.listenOnceAsync();
            }

            @Override
            public void sendActivityMessageAsync(String msg){
                speechSdk.sendActivityMessageAsync(msg);
            }

            @Override
            public String getSuggestedActions(){
                return gson.toJson(speechSdk.getSuggestedActions());
            }

            @Override
            public void clearSuggestedActions(){
                speechSdk.clearSuggestedActions();
            }

            @Override
            public void setConfiguration(String json) {
                Configuration configuration = gson.fromJson(json, new TypeToken<Configuration>(){}.getType());
                configurationManager.setConfiguration(configuration);
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

        // set up configuration for SpeechSdk
        configurationManager = new ConfigurationManager(this);

        Configuration configuration = configurationManager.getConfiguration();
        if (configuration.isEmpty()){
            // set up defaults
            configuration.serviceKey = DefaultConfiguration.SPEECH_SERVICE_SUBSCRIPTION_KEY;
            configuration.botId = DefaultConfiguration.DIRECT_LINE_SPEECH_SECRET_KEY;
            configuration.serviceRegion = DefaultConfiguration.SPEECH_SERVICE_SUBSCRIPTION_KEY_REGION;
            configuration.userName = DefaultConfiguration.USER_NAME;
            configuration.userId = DefaultConfiguration.USER_FROM_ID;
            configuration.locale = DefaultConfiguration.LOCALE;
            configuration.geolat = DefaultConfiguration.GEOLOCATION_LAT;
            configuration.geolon = DefaultConfiguration.GEOLOCATION_LON;
            configuration.historyLinecount = Integer.MAX_VALUE - 1;
            configurationManager.setConfiguration(configuration);
        }

        locationProvider = new LocationProvider(this, location -> {
            final String locLat = String.valueOf(location.getLatitude());
            final String locLon = String.valueOf(location.getLongitude());
            if (speechSdk != null) {
                speechSdk.sendLocationEvent(locLat, locLon);
            }
        });
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        EventBus.getDefault().unregister(this);
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if(intent != null) {
            String action = intent.getAction();
            if (action == null) action = ACTION_START_FOREGROUND_SERVICE;

            switch (action) {
                case ACTION_START_FOREGROUND_SERVICE:
                    Toast.makeText(getApplicationContext(), "Service is started", Toast.LENGTH_LONG).show();
                    startForegroundService();
                    break;
                case ACTION_STOP_FOREGROUND_SERVICE:
                    Toast.makeText(getApplicationContext(), "Service is stopped", Toast.LENGTH_LONG).show();
                    stopForegroundService();
                    break;
                case ACTION_START_LISTENING:
                    Toast.makeText(getApplicationContext(), "Listening", Toast.LENGTH_LONG).show();

                    if (speechSdk == null) initializeSpeechSdk(true);//assume true - for this to work the app must have been launched once for permission dialog
                    speechSdk.connectAsync();
                    speechSdk.listenOnceAsync();
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
        builder.setSmallIcon(R.mipmap.ic_launcher);
        Bitmap largeIconBitmap = BitmapFactory.decodeResource(getResources(), R.mipmap.ic_launcher);
        builder.setLargeIcon(largeIconBitmap);

        // Set notification priority
        builder.setPriority(NotificationManager.IMPORTANCE_LOW);

        // Make head-up notification
        builder.setFullScreenIntent(pendingIntent, true);

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

        startLocationUpdates();

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
            speechSdk.reset();
        }
        speechSdk = new SpeechSdk();
        File directory = getFilesDir();
        Configuration configuration = configurationManager.getConfiguration();
        speechSdk.initialize(configuration, haveRecordAudioPermission, directory.getPath());
    }

    // EventBus: the synthesizer has stopped playing
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventSynthesizerStopped(SynthesizerStopped event) {

        if(shouldListenAgain){
            shouldListenAgain = false;
            speechSdk.listenOnceAsync();
        }

    }

    // EventBus: the previous request timed out
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventRequestTimeout(RequestTimeout event) {
        broadcastTimeout(event);
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
    }

    // EventBus: received a response from Bot
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventActivityReceived(ActivityReceived activityReceived) throws IOException {
        if (activityReceived.botConnectorActivity != null) {
            BotConnectorActivity botConnectorActivity = activityReceived.botConnectorActivity;

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
                case "OpenDefaultApp":
                    Log.i(TAG_FOREGROUND_SERVICE, "OpenDefaultApp");
                    openDefaultApp(botConnectorActivity);
                    break;
                default:
                    broadcastWidgetUpdate(botConnectorActivity);
                    break;
            }

            // make the bot automatically listen again
            if(botConnectorActivity.getInputHint() != null){
                if(botConnectorActivity.getInputHint().equals(InputHints.EXPECTINGINPUT)){
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

    private void openDefaultApp(BotConnectorActivity botConnectorActivity){
        String intentStr = botConnectorActivity.getText();
        if (intentStr.startsWith("geo")){
            Uri intentUri = Uri.parse(intentStr);
            Intent mapIntent = new Intent(Intent.ACTION_VIEW, intentUri);
            mapIntent.setPackage("com.google.android.apps.maps");
            if (mapIntent.resolveActivity(getPackageManager()) != null) {
                startActivity(mapIntent);
            }
        }
        if (intentStr.startsWith("tel")){
            Uri intentUri = Uri.parse(intentStr);
            Intent dialerIntent = new Intent(Intent.ACTION_DIAL, intentUri);
            if (dialerIntent.resolveActivity(getPackageManager()) != null) {
                startActivity(dialerIntent);
            }
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
}
