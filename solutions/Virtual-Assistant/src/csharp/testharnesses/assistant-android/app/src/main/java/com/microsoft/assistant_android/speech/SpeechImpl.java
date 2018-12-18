package com.microsoft.assistant_android.speech;

import android.annotation.SuppressLint;
import android.content.Context;
import android.media.AudioManager;
import android.media.MediaPlayer;
import android.os.AsyncTask;
import android.os.SystemClock;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import android.util.Log;

import com.microsoft.cognitiveservices.speech.CancellationDetails;
import com.microsoft.cognitiveservices.speech.CancellationReason;
import com.microsoft.cognitiveservices.speech.ResultReason;
import com.microsoft.cognitiveservices.speech.SpeechConfig;
import com.microsoft.cognitiveservices.speech.SpeechRecognitionResult;
import com.microsoft.cognitiveservices.speech.SpeechRecognizer;

import java.util.Locale;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;

import static android.media.AudioManager.AUDIOFOCUS_GAIN_TRANSIENT;
import static android.media.AudioManager.AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK;
import static android.media.AudioManager.AUDIOFOCUS_LOSS;

public class SpeechImpl implements AudioManager.OnAudioFocusChangeListener{

    private static String TAG = "SpeechImpl";
    private TextToSpeech tts;
    private SpeechEventInterface _speechEventInterface;

    private UtteranceProgressListener mProgressListener = new UtteranceProgressListener() {
        @Override
        public void onStart(String utteranceId) {
            Log.e(TAG, "Utterance Progress Listener started");
        } // Do nothing

        @Override
        public void onError(String utteranceId) {
            _speechEventInterface.onSpeechError(utteranceId);
            Log.e("TTS Error", "TTS Error occurred");
        }

        @Override
        public void onDone(String utteranceId) {
            Log.e(TAG, "Utterance Progress Listener done");
        } // Do nothing
    };

    private boolean initialized;
    private String queuedText;
    private SpeechConfig _config = null;
    private SpeechRecognizer _reco = null;
    MediaPlayer recognizedKeyword = null;
    private long speechRecoTiming = -1;
    private boolean _recognizingForUser = false;
    public boolean ContinousRecognition = false;
    private String _speechKey = "";
    private String _speechRegion = "";
    private String _speechEdnpoint = "";

    public SpeechImpl(Context ctx, SpeechEventInterface event, int resource, String speechKey, String speechRegion, String speechEndpoint) {
        _speechKey = speechKey;
        _speechRegion = speechRegion;
        _speechEdnpoint = speechEndpoint;

        this._speechEventInterface = event;
        tts = new TextToSpeech(ctx, new TextToSpeech.OnInitListener() {
            @Override
            public void onInit(int status) {
                if (status == TextToSpeech.SUCCESS) {
                    initialized = true;
                    int langResult = tts.setLanguage(Locale.getDefault());

                    if (langResult == TextToSpeech.LANG_MISSING_DATA || langResult == TextToSpeech.LANG_NOT_SUPPORTED) {
                        tts.setLanguage(Locale.US);
                    }
                    _speechEventInterface.onInitialized();
                }
            }
        });
        tts.setOnUtteranceProgressListener(mProgressListener);

//        try {
//            _config = SpeechConfig.fromSubscription(speechKey, speechRegion);
//            assert(_config != null);
//
//        if (speechEndpoint != null & speechEndpoint.length() > 0)
//            _config.setEndpointId(speechEndpoint);
//
//            _reco = new SpeechRecognizer(_config);
//            assert(_reco != null);
//
//            _reco.recognized.addEventListener((s,e) -> {
//                String reco = e.getResult().getText();
//                if (reco != null && reco.length() > 0) {
//                    System.out.println("Recognized: " + reco);
//                    String recognizedModified;
//                    String lastChar;
//                    lastChar = reco.substring(reco.length()-1);
//                    if (lastChar.compareTo(".") == 0)
//                        recognizedModified = reco.substring(0,reco.length()-1);
//                    else
//                        recognizedModified = reco;
//                    _speechEventInterface.onSpeechRecognized(recognizedModified);
//                }
//            });
//        } catch (Exception ex) {
//            Log.e("SpeechSDKDemo", "unexpected " + ex.getMessage());
//            assert(false);
//        }
//        _config = SpeechConfig.fromSubscription(speechKey, speechRegion);
//        assert(_config != null);
//        if (speechEndpoint != null & speechEndpoint.length() > 0)
//            _config.setEndpointId(speechEndpoint);
//
//        _reco = new SpeechRecognizer(_config);
//
//        //set up event for recognizer
//        _reco.speechEndDetected.addEventListener((s,e) -> {
//            Log.d(TAG,"Speech end. Recognizing: " );
//
//        });
//        _reco.recognizing.addEventListener((o, intermediateResultEventArgs) -> {
//            final String s = intermediateResultEventArgs.getResult().getText();
//            _speechEventInterface.onSpeechRecognizing(s);
//        });
//        _reco.recognized.addEventListener((s,e) -> {
//            String reco = e.getResult().getText();
//            if (reco != null && reco.length() > 0) {
//                System.out.println("Recognized: " + reco);
//                if (ContinousRecognition) {
//                    // Calculate the time interval when the task is done
//                    long timeInterval = SystemClock.elapsedRealtime() - speechRecoTiming;
//                    Log.d(TAG, "Elapsed speech wait (ms): " + timeInterval);
//                    speechRecoTiming = -1;
//                    String recognizedModified;
//                    String lastChar;
//                    lastChar = reco.substring(reco.length()-1);
//                    if (lastChar.compareTo(".") == 0)
//                        recognizedModified = reco.substring(0,reco.length()-1);
//                    else
//                        recognizedModified = reco;
//                    _speechEventInterface.onSpeechRecognized(recognizedModified);
//                } else {
//                    if (reco.contains("Hey Buick")) {
//                        _recognizingForUser = true;
//                        recognizedKeyword.start();
//                        speechRecoTiming = SystemClock.elapsedRealtime();
//                        ContinousRecognition = true;
//                    }
//                }
//            }
//        });
//        _reco.sessionStarted.addEventListener((s,e) -> {
//            System.out.println("Speech session started");
//        });
//        _reco.sessionStopped.addEventListener((s,e) -> {
//            System.out.println("Speech session stopped");
//        });
//
//        recognizedKeyword = MediaPlayer.create(ctx, resource);
    }

    public boolean isSpeaking() {
        return tts.isSpeaking();
    }

    public void Pause() {
        if(tts !=null){
            tts.stop();
            tts.shutdown();
        }
    }
    public void StartSpeechReco() {
        try {
            SpeechConfig config = SpeechConfig.fromSubscription(_speechKey, _speechRegion);
            assert(config != null);

            SpeechRecognizer reco = new SpeechRecognizer(config);
            assert(reco != null);

            Future<SpeechRecognitionResult> task = reco.recognizeOnceAsync();
            assert(task != null);

            // Note: this will block the UI thread, so eventually, you want to
            //        register for the event (see full samples)
            SpeechRecognitionResult result = task.get();
            assert(result != null);

            if (result.getReason() == ResultReason.RecognizedSpeech) {
                _speechEventInterface.onSpeechRecognized(result.getText());
            }
            else {
                _speechEventInterface.onSpeechRecoError("Error recognizing. Did you update the subscription info?" + System.lineSeparator() + result.toString());
            }

            reco.close();
        } catch (Exception ex) {
            Log.e(TAG, "unexpected error in speech recognition: " + ex.getMessage());
            assert(false);
        }
    }
    @SuppressLint("StaticFieldLeak")
    public void Recognize() {
        new AsyncTask<Void, Void, Void>() {
            @Override
            protected Void doInBackground(Void... voids) {
                try {
                    Log.i(TAG, "onSpeechButtonClicked: start reco");

                    Future<SpeechRecognitionResult> task = _reco.recognizeOnceAsync();
                    assert(task != null);

                    // Note: this will block the UI thread, so eventually, you want to
                    //        register for the event (see full samples)
                    SpeechRecognitionResult result = task.get(10, TimeUnit.SECONDS);
                    assert(result != null);

                    if (result.getReason() == ResultReason.RecognizedSpeech) {
                        Log.d("Recognized","Text: " + result.toString());
                        _speechEventInterface.onSpeechRecognized(result.getText());
                    }
                    else if (result.getReason() == ResultReason.Canceled) {
                        CancellationDetails cancellation = CancellationDetails.fromResult(result);
                        if (cancellation.getReason() == CancellationReason.Error) {
                            _speechEventInterface.onSpeechRecoError(cancellation.getReason().toString());
                            Log.w("","Error recognizing. Reason: " + System.lineSeparator() + cancellation.getReason().toString() + System.lineSeparator() + cancellation.getErrorDetails());
                        }
                        else {
                            _speechEventInterface.onSpeechRecoError(cancellation.getReason().toString());
                            Log.w("","Error recognizing. Did you update the subscription info?" + System.lineSeparator() + cancellation.getReason().toString());
                        }

                    }

                    _reco.close();
                } catch (Exception ex) {
                    _speechEventInterface.onSpeechRecoError(ex.getMessage());
                    Log.e("SpeechSDKDemo", "unexpected " + ex.getMessage());
                    assert(false);
                }
                return null;
            }
        }.execute();
    }

    public void Speak(String message){
        try {
            if (message != null && message.length() > 0) {
                Log.d(TAG,"-->Speak");
                _reco.stopContinuousRecognitionAsync();
                while (tts.isSpeaking()) {
                    Thread.sleep(500);
                    //Log.d(TAG, "sleeping while speaking..");
                }
                new SpeakWorker().execute(message);
                Log.d(TAG,"<--Speak");
            }
        }
        catch (Exception e) {
            e.printStackTrace();
        }
    }

    private class SpeakWorker extends AsyncTask<String, Void, Void> {
        @Override
        protected Void doInBackground(String... params) {
            try {
                Log.d(TAG,"-->SpeakWorker::Speak");
                String message = params[0];
                if (message != null && message.length() > 0) {
                    Log.d(TAG, "In speaking:" + message);
                    tts.speak(message, TextToSpeech.QUEUE_FLUSH, null);
                    Thread.sleep(500);
                    while (tts.isSpeaking()) {
                        Thread.sleep(500);
                        //Log.d(TAG, "sleeping while speaking..");
                    }
                    _recognizingForUser = false;
                    _speechEventInterface.onDoneSpeaking();
                }
                else
                    Log.w(TAG,"No speech to speak");
            }
            catch (Exception ex) {
                Log.d(TAG,"Speech error");
                ex.printStackTrace();
            }
            Log.d(TAG,"<--SpeakWorker::Speak");
            return null;
        }
    }
    @Override
    public void onAudioFocusChange(int focusChange) {
        switch (focusChange) {
            case AUDIOFOCUS_GAIN_TRANSIENT:
                tts.stop();
            case AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK:
                tts.stop();
            case AUDIOFOCUS_LOSS:
                tts.stop();
            default:
                Log.w(TAG, String.format("onAudioFocusChange fired, unhandled / unknown value: %d", focusChange));
        }
    }
}
