package com.microsoft.bot.builder.solutions.directlinespeech;

import android.os.Handler;
import android.os.Looper;
import android.util.Log;

import com.google.gson.Gson;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.directlinespeech.utils.DateUtils;
import com.microsoft.cognitiveservices.speech.KeywordRecognitionModel;
import com.microsoft.cognitiveservices.speech.PropertyId;
import com.microsoft.cognitiveservices.speech.ServicePropertyChannel;
import com.microsoft.cognitiveservices.speech.SpeechRecognitionCanceledEventArgs;
import com.microsoft.cognitiveservices.speech.SpeechRecognitionResult;
import com.microsoft.cognitiveservices.speech.audio.AudioConfig;
import com.microsoft.cognitiveservices.speech.audio.PullAudioOutputStream;
import com.microsoft.cognitiveservices.speech.dialog.BotFrameworkConfig;
import com.microsoft.cognitiveservices.speech.dialog.CustomCommandsConfig;
import com.microsoft.cognitiveservices.speech.dialog.DialogServiceConfig;
import com.microsoft.cognitiveservices.speech.dialog.DialogServiceConnector;

import org.greenrobot.eventbus.EventBus;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.List;
import java.util.TimeZone;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

import client.model.ActivityTypes;
import client.model.CardAction;
import client.model.ChannelAccount;
import events.ActivityReceived;
import events.BotListening;
import events.Connected;
import events.Disconnected;
import events.GpsLocationSent;
import events.Recognized;
import events.RecognizedIntermediateResult;
import events.RequestTimeout;

import static com.microsoft.cognitiveservices.speech.ResultReason.RecognizedKeyword;
import static com.microsoft.cognitiveservices.speech.ResultReason.RecognizingKeyword;

public class SpeechSdk {

    // CONSTANTS
    private static final String LOGTAG = "SpeechSdk";
    public static final String SPEECHSDKLOGFILENAME = "SpeechSdk.log";
    public static final String APPLOGFILENAME = "app.log";
    private final int RESPONSE_TIMEOUT_PERIOD_MS = 15 * 1000;

    // STATE
    private MicrophoneStream microphoneStream;
    private DialogServiceConnector botConnector;
    private Synthesizer synthesizer;
    private Gson gson;
    private ChannelAccount from_user;
    private String localSpeechSdkLogPath;
    private String localAppLogFilePath;
    private String localLogDirectory;
    private boolean isConnected;
    private byte[] audioBuffer;
    private Configuration configuration;
    private Handler handler;
    private Runnable timeoutResponseRunnable;
    private ArrayList<CardAction> suggestedActions;
    private String dateSentLocationEvent;

    private File localSpeechSdkLogFile;
    private File localAppLogFile;
    private FileWriter streamWriter;

    public void initialize(Configuration configuration, boolean haveRecordAudioPermission, String localLogFileDirectory){
        audioBuffer = new byte[1024 * 2];
        suggestedActions = new ArrayList<>();
        gson = new Gson();
        this.configuration = configuration;
        synthesizer = new Synthesizer();
        //locale = Locale.getDefault().toString();
        from_user = new ChannelAccount();
        from_user.setName(configuration.userName);
        from_user.setId(configuration.userId);
        this.localSpeechSdkLogPath = localLogFileDirectory + "/" + SPEECHSDKLOGFILENAME;
        this.localAppLogFilePath = localLogFileDirectory + "/" + APPLOGFILENAME;
        this.localLogDirectory = localLogFileDirectory;
        localSpeechSdkLogFile = new File(localSpeechSdkLogPath);
        intializeAppLogFile();
        initializeSpeech(configuration, haveRecordAudioPermission);
        handler = new Handler(Looper.getMainLooper());
        if (configuration.currentTimezone != null) sendTimeZoneEvent(TimeZone.getTimeZone(configuration.currentTimezone));//only do this once per session
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

    private void logLongInfoMessage(String tag, String message){
        // Split by line, then ensure each line can fit into Log's maximum length.
        final int MAX_LOG_LENGTH = 4000;
        for (int i = 0, length = message.length(); i < length; i++) {
            int newline = message.indexOf('\n', i);
            newline = newline != -1 ? newline : length;
            do {
                int end = Math.min(newline, i + MAX_LOG_LENGTH);
                Log.i(tag, message.substring(i, end));
                i = end;
            } while (i < newline);
        }
    }

    private void initializeSpeech(Configuration configuration, boolean haveRecordAudioPermission){
        AudioConfig audioInput = null;
        if (haveRecordAudioPermission) audioInput = AudioConfig.fromDefaultMicrophoneInput();//AudioConfig.fromStreamInput(createMicrophoneStream());

        DialogServiceConfig dialogServiceConfig = createDialogServiceConfiguration();

        // Only needed for USB mic array. Ignored (i.e. safe) if usb audio is not used.
        // Linear mic array config:
        dialogServiceConfig.setProperty("DeviceGeometry", "Linear4");
        dialogServiceConfig.setProperty("SelectedGeometry", "Linear4");
        dialogServiceConfig.setProperty("CARBON-INTERNAL-PmaDumpAudioToFilePrefix", localLogDirectory +"/pma");
        botConnector = new DialogServiceConnector(dialogServiceConfig, audioInput);

        botConnector.recognizing.addEventListener((o, speechRecognitionResultEventArgs) -> {
            final String recognizedSpeech = speechRecognitionResultEventArgs.getResult().getText();

            if (speechRecognitionResultEventArgs.getResult().getReason().equals(RecognizingKeyword)) {
                // show listening animation when keyword is recognized
                EventBus.getDefault().post(new BotListening());
            }

            LogInfo("Intermediate result received: " + recognizedSpeech);

            // trigger callback to expose result in 3rd party app
            EventBus.getDefault().post(new RecognizedIntermediateResult(recognizedSpeech));
        });

        botConnector.recognized.addEventListener((o, speechRecognitionResultEventArgs) -> {
            final String recognizedSpeech = speechRecognitionResultEventArgs.getResult().getText();
            LogInfo("Final result received: " + recognizedSpeech);

            if (!speechRecognitionResultEventArgs.getResult().getReason().equals(RecognizedKeyword)) {
                // trigger callback to expose result in 3rd party app
                EventBus.getDefault().post(new Recognized(recognizedSpeech));
            }

            startResponseTimeoutTimer();
        });

        botConnector.sessionStarted.addEventListener((o, sessionEventArgs) -> {
            LogInfo("got a session (" + sessionEventArgs.getSessionId() + ") event: sessionStarted");
        });

        botConnector.sessionStopped.addEventListener((o, sessionEventArgs) -> {
            LogInfo("got a session (" + sessionEventArgs.getSessionId() + ") event: sessionStopped");
        });

        botConnector.canceled.addEventListener((Object o, SpeechRecognitionCanceledEventArgs canceledEventArgs) -> {
            // cancel reponse timeout timer ASAP
            cancelResponseTimeoutTimer();

            final int errCode = canceledEventArgs.getErrorCode().getValue();
            LogInfo("canceled with error code: "+ errCode +" ,also: "+ canceledEventArgs.getErrorDetails());

            switch (errCode) {
                case 5:// this is Connection was closed by the remote host. Error code: 1011. Error details: Unable to read data from the transport connection: Connection reset by peer
                case 1:// this is the authentication error (401) when using wrong certificate
                    isConnected = false;
                    EventBus.getDefault().post(new Disconnected(canceledEventArgs.getReason().getValue(), canceledEventArgs.getErrorDetails(), errCode));
                    break;
            }

        });

        botConnector.activityReceived.addEventListener((o, activityEventArgs) -> {
            final String json = activityEventArgs.getActivity();
            logLongInfoMessage(LOGTAG, "received activity: " + json);

            if (activityEventArgs.hasAudio()) {
                // cancel response timeout timer
                // note: located here because a lot of activity events are received,
                //       by putting it here, only one event (with speech) cancels the timer.
                cancelResponseTimeoutTimer();

                LogInfo("Activity Has Audio");
                PullAudioOutputStream outputStream = activityEventArgs.getAudio();
                synthesizer.playStream(outputStream);
            }

            activityReceived(json);
        });
    }

    private DialogServiceConfig createDialogServiceConfiguration() {
        DialogServiceConfig dialogServiceConfig;

        if (configuration.customCommandsAppId == null || configuration.customCommandsAppId.isEmpty()) {
            dialogServiceConfig = BotFrameworkConfig.fromSubscription(
                    configuration.speechSubscriptionKey,
                    configuration.speechRegion);
        } else {
            dialogServiceConfig = CustomCommandsConfig.fromSubscription(
                    configuration.customCommandsAppId,
                    configuration.speechSubscriptionKey,
                    configuration.speechRegion);
        }

        dialogServiceConfig.setProperty(PropertyId.Conversation_From_Id, from_user.getId());

        dialogServiceConfig.setProperty("SPEECH-RecoLanguage", configuration.srLanguage);
        if (!(configuration.customVoiceDeploymentIds == null || configuration.customVoiceDeploymentIds.isEmpty())) {
            dialogServiceConfig.setProperty(PropertyId.Conversation_Custom_Voice_Deployment_Ids, configuration.customVoiceDeploymentIds);
        }
        if (!(configuration.customSREndpointId == null || configuration.customSREndpointId.isEmpty())) {
            dialogServiceConfig.setServiceProperty("cid", configuration.customSREndpointId, ServicePropertyChannel.UriQueryParameter);
        }
        if (configuration.speechSdkLogEnabled) dialogServiceConfig.setProperty(PropertyId.Speech_LogFilename, localSpeechSdkLogFile.getAbsolutePath());;

        return dialogServiceConfig;
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

        if (botConnectorActivity != null) {

            if (botConnectorActivity.getSuggestedActions() != null && botConnectorActivity.getSuggestedActions().getActions() != null) {
                List<CardAction> actionList = botConnectorActivity.getSuggestedActions().getActions();
                suggestedActions.clear();
                suggestedActions.addAll(actionList);
            }

            EventBus.getDefault().post(new ActivityReceived(botConnectorActivity));
        } else {
            LogDebug("json error");
        }
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
        EventBus.getDefault().post(new BotListening());
        final Future<SpeechRecognitionResult> task = botConnector.listenOnceAsync();
        setOnTaskCompletedListener(task, result -> {
            // your code here
        });
    }

    public void startKeywordListeningAsync(InputStream inputStream, String keyword){
        LogInfo("startKeywordListeningAsync");
        try {
            final Future<Void> task = botConnector.startKeywordRecognitionAsync(KeywordRecognitionModel.fromStream(inputStream,keyword,false ));
            setOnTaskCompletedListener(task, result -> {
                LogInfo("startKeywordRecognition");
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
            LogInfo("stopKeywordRecognition");
        });
    }

    private void startResponseTimeoutTimer(){
        LogInfo("startResponseTimeoutTimer");
        if (timeoutResponseRunnable == null) {
            timeoutResponseRunnable = () -> {
                // reset state as if the previous request was received to let user make new request
                EventBus.getDefault().post(new RequestTimeout());
            };
        }

        handler.postDelayed(timeoutResponseRunnable, RESPONSE_TIMEOUT_PERIOD_MS);
    }

    private void cancelResponseTimeoutTimer(){
        LogInfo("cancelResponseTimeoutTimer");
        if (timeoutResponseRunnable != null && handler != null){
            handler.removeCallbacks(timeoutResponseRunnable);
        }
    }

    public void sendActivityMessageAsync(CharSequence chars) {
        LogInfo("sendActivityMessageAsync\n" + chars);
        if (botConnector != null) {

            final client.model.Activity activityTemplate = new client.model.Activity();
            activityTemplate.text((String)chars);
            activityTemplate.type(ActivityTypes.MESSAGE);
            if (from_user != null) activityTemplate.setFrom(from_user);

            final String activityJson = gson.toJson(activityTemplate);
            final Future<String> task = botConnector.sendActivityAsync(activityJson);
            setOnTaskCompletedListener(task, result -> {
                LogInfo("sendActivityAsync done");
                startResponseTimeoutTimer();
            });
        }
    }

    /*
     * Send the VA.Location event to the bot
     */
    public void sendLocationEvent(String latitude, String longitude) {
        String coordinates = latitude + "," + longitude;
        client.model.Activity activityTemplate = createEventActivity("VA.Location", null, coordinates);
        if (from_user != null) activityTemplate.setFrom(from_user);

        final String activityJson = gson.toJson(activityTemplate);
        final Future<String> task = botConnector.sendActivityAsync(activityJson);
        setOnTaskCompletedListener(task, result -> {
            LogInfo("sendLocationEvent done: "+activityJson);
            dateSentLocationEvent = DateUtils.getCurrentTime();
            EventBus.getDefault().post(new GpsLocationSent(latitude, longitude));
        });
    }

    /*
     * Send the VA.TimeZone event to the bot
     */
    private void sendTimeZoneEvent(TimeZone tz) {
        client.model.Activity activityTemplate = createEventActivity("VA.Timezone", null, tz.getDisplayName());

        final String activityJson = gson.toJson(activityTemplate);
        final Future<String> task = botConnector.sendActivityAsync(activityJson);
        setOnTaskCompletedListener(task, result -> {
            LogDebug("sendActivityAsync done: "+activityJson);
        });
    }

    public void disconnectAsync() {
        cancelResponseTimeoutTimer();
        isConnected = false;
        stopKeywordListening();
        final Future<Void> task = botConnector.disconnectAsync();
    }

    public String getDateSentLocationEvent() {
        return dateSentLocationEvent;
    }

    public Synthesizer getSynthesizer() { return synthesizer; }

    public ArrayList<CardAction> getSuggestedActions() {
        return suggestedActions;
    }

    public void clearSuggestedActions() {
        suggestedActions.clear();
    }

    public void requestWelcomeCard() {
//        from: user object,
//        name: 'startConversation',
//        type: 'event'
//        "value":""
        if (botConnector != null) {

            final client.model.Activity activityTemplate = new client.model.Activity();
            activityTemplate.name("startConversation");
            activityTemplate.type(ActivityTypes.EVENT);
            if (from_user != null) activityTemplate.setFrom(from_user);
            activityTemplate.setValue("");

            final String activityJson = gson.toJson(activityTemplate);
            final Future<String> task = botConnector.sendActivityAsync(activityJson);
            setOnTaskCompletedListener(task, result -> {
                LogDebug("requestWelcomeCard done: "+activityJson);
            });
        }
    }

    /*
     * Create Event Activity with inputs: name, channel data, and value
     */
    private client.model.Activity createEventActivity(String eventname, Object channelData, Object value) {
        client.model.Activity activity = new client.model.Activity();
        activity.setType(ActivityTypes.EVENT);
        activity.setLocale(configuration.srLanguage);
        if (from_user != null) activity.setFrom(from_user);
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
