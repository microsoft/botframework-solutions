package com.microsoft.bot.builder.solutions.directlinespeech;

import android.util.Log;

import com.google.gson.Gson;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.cognitiveservices.speech.KeywordRecognitionModel;
import com.microsoft.cognitiveservices.speech.SpeechRecognitionCanceledEventArgs;
import com.microsoft.cognitiveservices.speech.audio.AudioConfig;
import com.microsoft.cognitiveservices.speech.audio.PullAudioOutputStream;
import com.microsoft.cognitiveservices.speech.dialog.BotConnectorActivity;
import com.microsoft.cognitiveservices.speech.dialog.BotConnectorConfig;
import com.microsoft.cognitiveservices.speech.dialog.SpeechBotConnector;

import org.greenrobot.eventbus.EventBus;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStream;
import java.util.TimeZone;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

import client.model.Activity;
import client.model.ActivityTypes;
import client.model.ChannelAccount;
import events.ActivityReceived;
import events.Connected;
import events.Disconnected;
import events.Recognized;
import events.RecognizedIntermediateResult;

public class SpeechSdk {

    // CONSTANTS
    private static final String LOGTAG = "SpeechSdk";
    public static final String CARBONLOGFILENAME = "carbon.log";
    public static final String APPLOGFILENAME = "app.log";

    // STATE
    private MicrophoneStream microphoneStream;
    private SpeechBotConnector botConnector;
    private Synthesizer synthesizer;
    private Gson gson;
    private ChannelAccount from_user;
    private String localCarbonLogFilePath;
    private String localAppLogFilePath;
    private String localLogDirectory;
    private boolean isConnected;
    private byte[] audioBuffer;
    private Configuration configuration;

    private File localAppLogFile;
    private FileWriter streamWriter;

    public void initialize(Configuration configuration, boolean haveRecordAudioPermission, String localLogFileDirectory){
        audioBuffer = new byte[1024 * 2];
        gson = new Gson();
        this.configuration = configuration;
        synthesizer = new Synthesizer();
        //locale = Locale.getDefault().toString();
        from_user = new ChannelAccount();
        from_user.setName(configuration.userName);
        from_user.setId(configuration.userId);
        this.localCarbonLogFilePath = localLogFileDirectory + "/" + CARBONLOGFILENAME;
        this.localAppLogFilePath = localLogFileDirectory + "/" + APPLOGFILENAME;
        this.localLogDirectory = localLogFileDirectory;
        intializeAppLogFile();
        initializeSpeech(configuration, haveRecordAudioPermission);
    }

    private void intializeAppLogFile() {
        localAppLogFile = new File(localAppLogFilePath);
        try {
            streamWriter = new FileWriter(localAppLogFile);
        }catch(IOException e)
        {
            Log.e(LOGTAG, e.getMessage());
        }
    }

    private void LogException(String message){
        Log.e(LOGTAG, message);
        LogToFile(message);
    }

    private void LogDebug(String message){
        Log.d(LOGTAG, message);
        LogToFile(message);
    }

    private void LogInfo(String message){
        Log.i(LOGTAG, message);
        LogToFile(message);
    }

    private void LogToFile(String message){
        if (localAppLogFile != null){
            try {
                streamWriter.write(message +"\n");
                streamWriter.flush();
            }catch(IOException e){
                Log.e(LOGTAG, e.getMessage());
            }
        }
    }

    private void initializeSpeech(Configuration configuration, boolean haveRecordAudioPermission){
        AudioConfig audioInput = null;
        if (haveRecordAudioPermission) audioInput = AudioConfig.fromDefaultMicrophoneInput();//AudioConfig.fromStreamInput(createMicrophoneStream());

        BotConnectorConfig botConfig = BotConnectorConfig.fromSecretKey(
                configuration.botId,
                configuration.serviceKey,
                configuration.serviceRegion);
        botConfig.setProperty("SPEECH-RecoLanguage", configuration.locale);


        // Only needed for USB mic array. Ignored (i.e. safe) if usb audio is not used.
        // Linear mic array config:
        botConfig.setProperty("DeviceGeometry", "Linear4");
        botConfig.setProperty("SelectedGeometry", "Linear4");
        botConfig.setProperty("CARBON-INTERNAL-PmaDumpAudioToFilePrefix", localLogDirectory +"/pma");
        botConnector = new SpeechBotConnector(botConfig, audioInput);

        botConnector.recognizing.addEventListener((o, speechRecognitionResultEventArgs) -> {
            final String recognizedSpeech = speechRecognitionResultEventArgs.getResult().getText();
            LogInfo("Intermediate result received: " + recognizedSpeech);

            // trigger callback to expose result in 3rd party app
            EventBus.getDefault().post(new RecognizedIntermediateResult(recognizedSpeech));
        });

        botConnector.recognized.addEventListener((o, speechRecognitionResultEventArgs) -> {
            final String recognizedSpeech = speechRecognitionResultEventArgs.getResult().getText();
            LogInfo("Final result received: " + recognizedSpeech);

            // trigger callback to expose result in 3rd party app
            EventBus.getDefault().post(new Recognized(recognizedSpeech));
        });

        botConnector.sessionStarted.addEventListener((o, sessionEventArgs) -> {
            LogInfo("got a session (" + sessionEventArgs.getSessionId() + ") event: sessionStarted");
        });

        botConnector.sessionStopped.addEventListener((o, sessionEventArgs) -> {
            LogInfo("got a session (" + sessionEventArgs.getSessionId() + ") event: sessionStopped");
        });

        botConnector.canceled.addEventListener((Object o, SpeechRecognitionCanceledEventArgs canceledEventArgs) -> {
            final int errCode = canceledEventArgs.getErrorCode().getValue().swigValue();
            LogInfo("canceled with error code: "+ errCode +" ,also: "+ canceledEventArgs.getErrorDetails());

            switch (errCode) {
                case 5:// this is Connection was closed by the remote host. Error code: 1011. Error details: Unable to read data from the transport connection: Connection reset by peer
                case 1:// this is the authentication error (401) when using wrong certificate
                    isConnected = false;
                    EventBus.getDefault().post(new Disconnected(canceledEventArgs.getErrorDetails(), errCode));
                    break;
            }

        });

        botConnector.activityReceived.addEventListener((o, activityEventArgs) -> {
            final String json = activityEventArgs.getActivity().serialize();
            LogInfo("received activity: " + json);

            if (activityEventArgs.hasAudio()) {
                LogInfo("Activity Has Audio");
                PullAudioOutputStream outputStream = activityEventArgs.getAudio();
                synthesizer.playStream(outputStream);
            }

            activityReceived(json);
        });
    }

    /**
     * used by activityReceived event listener
     * Exposed public to emulate sending activity json
     * @param activityJson
     * @see client.model.BotConnectorActivity
     */
    public void activityReceived(String activityJson){
        // trigger callback to expose result in 3rd party app

        client.model.BotConnectorActivity botConnectorActivity = gson.fromJson(activityJson, client.model.BotConnectorActivity.class);
        EventBus.getDefault().post(new ActivityReceived(botConnectorActivity));
    }

    private MicrophoneStream createMicrophoneStream() {
        if (microphoneStream != null) {
            microphoneStream.close();
            microphoneStream = null;
        }

        microphoneStream = new MicrophoneStream();
        return microphoneStream;
    }

    public void connectAsync(){
        Future<Void> task = botConnector.connectAsync();
        setOnTaskCompletedListener(task, result -> {
            // your code here
            LogDebug("connectAsync");
            isConnected = true;
            EventBus.getDefault().post(new Connected());
        });
    }

    public void listenOnceAsync(){
        LogInfo("listenOnceAsync");
        final Future<Void> task = botConnector.listenOnceAsync();
        setOnTaskCompletedListener(task, result -> {
            // your code here
        });
    }

    public void startKeywordListeningAsync(InputStream inputStream, String keyword){ ;
        LogInfo("startKeywordListeningAsync");
        try {
            final Future<Void> task = botConnector.startKeywordRecognitionAsync(KeywordRecognitionModel.fromStream(inputStream,keyword,false ));
            setOnTaskCompletedListener(task, result -> {
                // your code here
            });
        }
        catch (FileNotFoundException e){
            LogException("Keyword file not found " + e.getMessage());
        }
        catch (IOException e){
            LogException(e.getMessage());
        }
    }

    public void stopKeywordListening(){
        final Future<Void> task = botConnector.stopKeywordRecognitionAsync();
        setOnTaskCompletedListener(task, result -> {
            // your code here
        });
    }

    public void disconnectAsync(){
        isConnected = false;
        LogInfo("disconnectAsync");
        final Future<Void> task = botConnector.disconnectAsync();
        setOnTaskCompletedListener(task, result -> {
            // your code here
            LogInfo("disconnected done");
        });
    }

    public void sendActivityMessageAsync(CharSequence chars) {
        LogInfo("sendActivityMessageAsync\n" + chars);
        if (botConnector != null) {

            final Activity activityTemplate = new Activity();
            activityTemplate.text((String)chars);
            activityTemplate.type(ActivityTypes.MESSAGE);
            if (from_user != null) activityTemplate.setFrom(from_user);

            final String activityJson = gson.toJson(activityTemplate);
            BotConnectorActivity activity = BotConnectorActivity.fromSerializedActivity(activityJson);

            final Future<Void> task = botConnector.sendActivityAsync(activity);
            setOnTaskCompletedListener(task, result -> {
                LogInfo("sendActivityAsync done");
            });
        }
    }

    /*
     * Send the IPA.Location event to the bot
     */
    public void sendLocationEvent(String latitude, String longitude) {
        String coordinates = latitude + "," + longitude;
        Activity activityTemplate = createEventActivity("IPA.Location", null, coordinates);
        if (from_user != null) activityTemplate.setFrom(from_user);

        final String activityJson = gson.toJson(activityTemplate);
        BotConnectorActivity activity = BotConnectorActivity.fromSerializedActivity(activityJson);

        final Future<Void> task = botConnector.sendActivityAsync(activity);
        setOnTaskCompletedListener(task, result -> {
            LogInfo("sendLocationEvent done: "+activityJson);
        });
    }

    /*
     * Send the IPA.TimeZone event to the bot
     */
    public void sendTimeZoneEvent() {
        TimeZone tz = TimeZone.getDefault();
        Activity activityTemplate = createEventActivity("IPA.Timezone", null, tz.getDisplayName());

        final String activityJson = gson.toJson(activityTemplate);
        BotConnectorActivity activity = BotConnectorActivity.fromSerializedActivity(activityJson);

        final Future<Void> task = botConnector.sendActivityAsync(activity);
        setOnTaskCompletedListener(task, result -> {
            LogDebug("sendActivityAsync done: "+activityJson);
        });
    }

    public void reset() {
        isConnected = false;
        stopKeywordListening();
        final Future<Void> task = botConnector.disconnectAsync();
    }

    public void resetBot() {
        isConnected = false;
        final Future<Void> task = botConnector.disconnectAsync();
        setOnTaskCompletedListener(task, result -> {
            Log.d(LOGTAG,"disconnected");
            connectAsync();
        });
    }

    public void requestWelcomeCard() {
//        from: user object,
//        name: 'startConversation',
//        type: 'event'
//        "value":""
        if (botConnector != null) {

            final Activity activityTemplate = new Activity();
            activityTemplate.name("startConversation");
            activityTemplate.type(ActivityTypes.EVENT);
            if (from_user != null) activityTemplate.setFrom(from_user);
            activityTemplate.setValue("");

            final String activityJson = gson.toJson(activityTemplate);
            BotConnectorActivity activity = BotConnectorActivity.fromSerializedActivity(activityJson);

            final Future<Void> task = botConnector.sendActivityAsync(activity);
            setOnTaskCompletedListener(task, result -> {
                LogDebug("requestWelcomeCard done: "+activityJson);
            });
        }
    }

    /*
     * Create Event Activity with inputs: name, channel data, and value
     */
    private Activity createEventActivity(String eventname, Object channelData, Object value) {
        Activity activity = new Activity();
        activity.setType(ActivityTypes.EVENT);
        activity.setLocale(configuration.locale);
        if (from_user != null) activity.setFrom(from_user);
        activity.setChannelId(configuration.directlineConstant);
        activity.setChannelData(channelData);
        activity.setName(eventname);
        activity.setValue(value);

        return activity;
    }

    private <T> void setOnTaskCompletedListener(Future<T> task, OnTaskCompletedListener<T> listener) {
        s_executorService.submit(() -> {
            T result = task.get();
            listener.onCompleted(result);
            return null;
        });
    }

    private interface OnTaskCompletedListener<T> {
        void onCompleted(T taskResult);
    }

    private static ExecutorService s_executorService;
    static {
        s_executorService = Executors.newCachedThreadPool();
    }
}
