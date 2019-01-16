//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
package com.microsoft.bot.solutions.speechdevices.samples.botapp;

import android.os.AsyncTask;
import android.text.TextUtils;
import android.util.Log;
import com.microsoft.cognitiveservices.speech.KeywordRecognitionModel;
import com.microsoft.cognitiveservices.speech.SpeechConfig;
import com.microsoft.cognitiveservices.speech.SpeechRecognizer;
import com.microsoft.cognitiveservices.speech.audio.AudioConfig;
import io.swagger.client.model.Activity;
import io.swagger.client.model.InputHints;
import static io.swagger.client.model.InputHints.ACCEPTINGINPUT;
import static io.swagger.client.model.InputHints.EXPECTINGINPUT;

public class SpeechHandler {
    private MainActivity mainActivity;

    public static SpeechRecognizer recognizer;
    private InputHints currentInputHint = InputHints.IGNORINGINPUT;
    private static final String logTag_kws = "keyword_rec";
    private static final String logTag_con = "continuous_rec";

    public SpeechHandler(MainActivity mainactivity){
        this.mainActivity = mainactivity;

        try {
            SpeechConfig speechConfig = SpeechConfig.fromSubscription(Configuration.SpeechSubscriptionKey, Configuration.SpeechRegion);

            // PMA parameters
            speechConfig.setProperty("DeviceGeometry", Configuration.DeviceGeometry);
            speechConfig.setProperty("SelectedGeometry", Configuration.SelectedGeometry);
            speechConfig.setSpeechRecognitionLanguage(Configuration.Locale);
        } catch (Exception ex) {
            System.out.println(ex.getMessage());
            return;
        }
    }

    /*
    Disable the active Speech Recognizer
     */
    public class DisableRecognizersAsync extends AsyncTask<Void, Void, Void> {
        @Override
        protected Void doInBackground(Void... params) {
            switch(currentInputHint){
                case ACCEPTINGINPUT:
                    new StopKeywordRecognizerAsync(false).execute();
                    break;
                case EXPECTINGINPUT:
                    new StopContinuousRecognizerAsync(false).execute();
                    break;
            }

            return null;
        }

        @Override
        protected void onPostExecute(Void result) {
            mainActivity.clearTextBox();
            mainActivity.recognizedTextContent.set(0, mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_notlistening));
            mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));

        }
    }

    private AudioConfig getAudioConfig() {
        // run from the microphone
        return AudioConfig.fromDefaultMicrophoneInput();
    }

    private SpeechConfig getSpeechConfig() {
        SpeechConfig speechConfig = SpeechConfig.fromSubscription(Configuration.SpeechSubscriptionKey, Configuration.SpeechRegion);

        // PMA parameters
        speechConfig.setProperty("DeviceGeometry", Configuration.DeviceGeometry);
        speechConfig.setProperty("SelectedGeometry", Configuration.SelectedGeometry);
        speechConfig.setSpeechRecognitionLanguage(Configuration.Locale);

        return speechConfig;
    }

    /*
    Initialize a new Keyword Recognizer and set listener events
     */
    public class EnableKeywordRecognizerAsync extends AsyncTask<Void, Void, Void>{

        @Override
        protected void onPreExecute(){
            recognizer = new SpeechRecognizer(getSpeechConfig(), getAudioConfig());


            recognizer.sessionStarted.addEventListener((o, sessionEventArgs) -> {
                mainActivity.clearTextBox();
                mainActivity.recognizedTextContent.set(0, Configuration.Keyword + " " +  mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_keyword_detected));
                mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));
            });


            recognizer.recognizing.addEventListener((o, intermediateResultEventArgs) -> {
                final String intermediateString = intermediateResultEventArgs.getResult().getText();
                HandleRecognizerIntermediateResult(intermediateString, logTag_kws);
            });

            recognizer.recognized.addEventListener((o, finalResultEventArgs) -> {
                String finalString = finalResultEventArgs.getResult().getText();
                if (!finalString.isEmpty()) {
                    HandleRecognizerFinalResult(finalString);
                    mainActivity.clearTextBox();
                    mainActivity.recognizedTextContent.set(0, mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_keyword) + " " + Configuration.Keyword + mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.ellipses));
                    mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));
                }
            });
        }

        @Override
        protected Void doInBackground(Void... params) {
            if(recognizer != null){
                recognizer.startKeywordRecognitionAsync(KeywordRecognitionModel.fromFile(Configuration.KeywordModel));
            }
            return null;
        }

        @Override
        protected void onPostExecute(Void result) {
            currentInputHint = ACCEPTINGINPUT;
            mainActivity.clearTextBox();
            mainActivity.recognizedTextContent.set(0, mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_keyword) + " " + Configuration.Keyword + mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.ellipses));
            mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));
        }
    }

    /*
    Stop Keyword Recognizer
     */
    public class StopKeywordRecognizerAsync extends AsyncTask<Void, Void, Void>{

        private Boolean enableContinuousOnPost;

        public StopKeywordRecognizerAsync(Boolean enableContinous){
            enableContinuousOnPost = enableContinous;
        }


        @Override
        protected Void doInBackground(Void... params) {
            if(recognizer != null){
                recognizer.stopKeywordRecognitionAsync();
            }
            return null;
        }

        @Override
        protected void onPostExecute(Void result) {
            recognizer = null;
            currentInputHint = InputHints.IGNORINGINPUT;
            Log.i(logTag_kws, "Keyword recognition stopped.");


            if(enableContinuousOnPost){
                new EnableContinuousRecognizerAsync().execute();
            } else {
                mainActivity.clearTextBox();
                mainActivity.recognizedTextContent.set(0, mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_notlistening));
                mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));
            }
        }
    }

    /*
     Initialize a new Continuous Recognizer and set listener events
     */
    public class EnableContinuousRecognizerAsync extends AsyncTask<Void, Void, Void>{
        @Override
        protected void onPreExecute(){
            recognizer = new SpeechRecognizer(getSpeechConfig(), getAudioConfig());

            recognizer.recognizing.addEventListener((o, speechRecognitionResultEventArgs) -> {
                final String intermediateString = speechRecognitionResultEventArgs.getResult().getText();
                HandleRecognizerIntermediateResult(intermediateString, logTag_con);
            });

            recognizer.recognized.addEventListener((o, speechRecognitionResultEventArgs) -> {
                String finalString = speechRecognitionResultEventArgs.getResult().getText();
                if (!finalString.isEmpty()) {
                    // TODO: temporary placeholder
                    // Microphone needs to be disabled after utterance in continuous mode
                    // so that it doesn't hear bot's immediate response

                    new StopContinuousRecognizerAsync(false).execute();

                    HandleRecognizerFinalResult(finalString);
                }
            });
        }

        @Override
        protected Void doInBackground(Void... params) {
            if(recognizer != null){
                recognizer.startContinuousRecognitionAsync();
            }
            return null;
        }

        @Override
        protected void onPostExecute(Void result) {
            currentInputHint = EXPECTINGINPUT;
            mainActivity.clearTextBox();
            mainActivity.recognizedTextContent.set(0, mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_listening));
            mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));
        }
    }


    /*
    Stop Continuous Recognizer
    */
    public class StopContinuousRecognizerAsync extends AsyncTask<Void, Void, Void>{
        private Boolean enableKeywordOnPost;

        public StopContinuousRecognizerAsync(Boolean enableKeyword){
            enableKeywordOnPost = enableKeyword;
        }

        @Override
        protected Void doInBackground(Void... params) {
            if(recognizer != null){
                recognizer.stopContinuousRecognitionAsync();
            }
            return null;
        }

        @Override
        protected void onPostExecute(Void result) {
            recognizer = null;
            currentInputHint = InputHints.IGNORINGINPUT;
            Log.i(logTag_kws, "Continuous recognition stopped.");

            if(enableKeywordOnPost){
                new EnableKeywordRecognizerAsync().execute();
            } else{
                mainActivity.clearTextBox();
                mainActivity.recognizedTextContent.set(0, mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_notlistening));
                mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));
            }
        }
    }

    /*
    Update RecognizedTextBox with intermediate recognizer result as it is processed
     */
    public void HandleRecognizerIntermediateResult(String s, String logTag_con) {
        Log.i(logTag_con, "Intermediate result: " + s);
        mainActivity.clearTextBox();
        mainActivity.recognizedTextContent.set(0, s);
        mainActivity.UpdateInputText(TextUtils.join(mainActivity.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), mainActivity.recognizedTextContent));
    }

    /*
    Send final recognizer result to the bot application
     */
    public void HandleRecognizerFinalResult(String s) {
        String query = s.replaceAll("(?i)" + Configuration.Keyword, "");
        // Remove punctuation added by speech recognizer
        query = query.replaceAll("[。，]", "");

        try {
            if(query.length() != 0){
                Activity userMessageActivity = mainActivity.directLineClient.CreateMessageActivityFromUser(query, null);

                mainActivity.runOnUiThread(() -> {
                    mainActivity.messageAdapter.add(userMessageActivity);
                });

                mainActivity.directLineClient.SendActivity(userMessageActivity);
            }
        } catch (Exception ex) {
            System.out.println(ex.getMessage());
        }
    }

    /*
    Set recognition mode based on new input hint from bot
     */
    public class UpdateInputHintStatusAsync extends AsyncTask<Void, Void, Void> {
        private InputHints newInputHint;

        public UpdateInputHintStatusAsync(InputHints inputHint){
            newInputHint = inputHint;
        }

        @Override
        protected void onPreExecute(){
        }

        @Override
        protected Void doInBackground(Void... params) {
            if(newInputHint == null) {
                newInputHint = InputHints.ACCEPTINGINPUT;
            }

            if(newInputHint != null){
                switch(newInputHint){
                    case ACCEPTINGINPUT:

                        switch(currentInputHint){
                            case EXPECTINGINPUT:
                                new StopContinuousRecognizerAsync(true).execute();
                                break;
                            case IGNORINGINPUT:
                                new EnableKeywordRecognizerAsync().execute();
                                break;
                        }
                        break;
                    case EXPECTINGINPUT:

                        switch(currentInputHint){
                            case ACCEPTINGINPUT:
                                new StopKeywordRecognizerAsync(true).execute();
                                break;
                            case IGNORINGINPUT:
                                new EnableContinuousRecognizerAsync().execute();
                                break;
                        }
                        break;
                    case IGNORINGINPUT:
                        new DisableRecognizersAsync().execute();
                        break;
                }
            }


            return null;
        }

        @Override
        protected void onPostExecute(Void result) {
            if(newInputHint != null){
                currentInputHint = newInputHint;
            }
        }
    }


}
