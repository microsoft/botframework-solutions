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

import android.content.Intent;
import android.content.SharedPreferences;
import android.os.AsyncTask;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.support.v7.app.AlertDialog;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.Toolbar;
import android.text.Layout;
import android.text.TextUtils;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.ListView;
import android.widget.TextView;

import org.json.JSONException;
import org.json.simple.parser.ParseException;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import io.swagger.client.JSON;
import io.swagger.client.WebSocketServerConnection;
import io.swagger.client.WebSocketServerConnection.ConnectionStatus;
import io.swagger.client.model.Activity;
import io.swagger.client.model.ActivitySet;
import io.swagger.client.model.ActivityTypes;
import io.swagger.client.model.Attachment;
import io.swagger.client.model.ChannelAccount;
import io.swagger.client.model.InputHints;

public class MainActivity extends AppCompatActivity implements WebSocketServerConnection.ServerListener{
    private TextView recognizedTextView;
    private Button startConversationButton;
    private ListView messageListView;
    public MessageAdapter messageAdapter;
    public ArrayList<String> recognizedTextContent = new ArrayList<>();
    private SharedPreferences preferences = null;
    private SpeechHandler speechHandler = null;

    private final String websocket_log = "Websocket";

    // Speech integration
    private static SpeechSynthesizer speechSynthesizer = null;
    public BotApplication.DirectLineClient directLineClient;
    private WebSocketServerConnection mServerConnection;
    private JSON json = new JSON();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.layout.activity_main);

        Toolbar toolbar = findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.toolbar);
        setSupportActionBar(toolbar);

        messageListView = findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.listview_message_list);
        messageAdapter = new MessageAdapter(this);
        messageListView.setAdapter(messageAdapter);
        recognizedTextView = findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.recognizedText);
        recognizedTextContent.add("");
        startConversationButton = findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.buttonStartConversation);
        PreferenceManager.setDefaultValues(this, com.microsoft.bot.solutions.speechdevices.samples.botapp.R.xml.pref_main, false);
        preferences = PreferenceManager.getDefaultSharedPreferences(this);
        Configuration.UpdateConfigurationWithSharedPreferences(this, preferences);

        if (Configuration.CheckForMissingConfiguration()){
            ShowAlertForMissingConfiguration();
        }

        directLineClient = new BotApplication.DirectLineClient();
        speechSynthesizer = new SpeechSynthesizer();
        speechHandler = new SpeechHandler(this);

        ///////////////////////////////////////////////////
        // Start or resume bot conversation and enable default microphone setting
        ///////////////////////////////////////////////////
        startConversationButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                final Button clickedButton = (Button) view;

                if (speechHandler.recognizer != null) {
                    speechHandler.new UpdateInputHintStatusAsync(InputHints.IGNORINGINPUT).execute();

                    MainActivity.this.runOnUiThread(() -> {
                        clickedButton.setText(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.button_resume));
                    });

                    return;
                }

                clearTextBox();

                recognizedTextContent.clear();
                recognizedTextContent.add("");

                if(directLineClient.conversation == null){
                    recognizedTextContent.set(0, getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.status_conversationstart));
                    UpdateInputText(TextUtils.join(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.delimiter), recognizedTextContent));
                    Configuration.UpdateConfigurationWithSharedPreferences(MainActivity.this, preferences);
                    speechSynthesizer = new SpeechSynthesizer();
                    new CreateDirectLineConversationAsync().execute();
                    new SendStartupEventsToDirectLineConversationAsync().execute();
                    speechHandler.new UpdateInputHintStatusAsync(Configuration.DefaultInputHint).execute();
                } else {
                    speechHandler.new UpdateInputHintStatusAsync(InputHints.ACCEPTINGINPUT).execute();
                }

                MainActivity.this.runOnUiThread(() -> {
                    clickedButton.setText(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.button_pause));
                });
            }
        });
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.menu.menu_main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        int id = item.getItemId();

        if (id == com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.action_settings) {
            // launch settings activity
            if (speechHandler.recognizer != null) {
                speechHandler.new UpdateInputHintStatusAsync(InputHints.IGNORINGINPUT).execute();

                MainActivity.this.runOnUiThread(() -> {
                    startConversationButton.setText(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.button_resume));
                });
            }

            startActivity(new Intent(MainActivity.this, SettingsActivity.class));
            return true;
        } else if(id == com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.action_startover){
            MainActivity.this.recreate();

            //StartOverConversation();
        }

        return super.onOptionsItemSelected(item);
    }

    /*
    Check for any changes to saved configuration & clear conversation if they exist.
    If coordinate configuration has been updated, send a new location event to the bot
     */
    @Override
    protected void onResume() {
        super.onResume();

        if(Configuration.CheckForUpdatedPreferences(preferences)){
            StartOverConversation();
        } else if(Configuration.CheckForUpdatedCoordinates(preferences)){
            Configuration.UpdateConfigurationWithSharedPreferences(this, preferences);
            try {
                directLineClient.SendVirtualAssistantLocationEvent(Configuration.Latitude, Configuration.Longitude);
            } catch (Exception ex) {
                System.out.println(ex.getMessage());
            }
        }

        if(mServerConnection != null){
            initializeServerConnection();
        }
    }

    private void StartOverConversation() {
        if(directLineClient.conversation != null){
            ShowAlertForConversationStartOver();
        }
    }

    private void ShowAlertForConversationStartOver() {
        AlertDialog.Builder alertDialog = new AlertDialog.Builder(MainActivity.this);
        alertDialog.setTitle(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.alert_startover_title));
        alertDialog.setMessage(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.alert_startover_message));
        alertDialog.setNeutralButton(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.alert_startover_neutralbutton), (dialog, which) -> MainActivity.this.recreate());
        alertDialog.show();
    }


    private void ShowAlertForMissingConfiguration() {
        AlertDialog.Builder alertDialog = new AlertDialog.Builder(MainActivity.this);
        alertDialog.setTitle(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.alert_startover_title));
        alertDialog.setMessage(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.alert_missingconfig_message));
        alertDialog.setNeutralButton(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.alert_startover_neutralbutton), (dialog, which) -> MainActivity.this.finish());
        alertDialog.show();
    }

    @Override
    protected void onPause() {
        super.onPause();
        if(mServerConnection != null) {
            mServerConnection.Disconnect();
        }
    }

    @Override
    public void onNewMessage(String message) {
        try {
            if(message != null || message != "") {
                Log.i(websocket_log,"Received message:" + message);

                ActivitySet activitySet = json.deserialize(message, ActivitySet.class);
                if(activitySet != null){
                    ParseBotActivitiesToSpeech(activitySet.getActivities());
                }
            }
        }
        catch (Exception ex)
        {
            Log.d(websocket_log, ex.getMessage());
        }
    }

    @Override
    public void onStatusChange(ConnectionStatus status) {
        Log.d(websocket_log, status.name());
    }

    private void initializeServerConnection(){
        mServerConnection.Connect(this);
    }

    //region UI

    public void clearTextBox() {
        UpdateInputText("");
    }

    public void UpdateInputText(final String s) {
        MainActivity.this.runOnUiThread(() -> {
            recognizedTextView.setText(s);

            final Layout layout = recognizedTextView.getLayout();
            if (layout != null) {
                int scrollDelta = layout.getLineBottom(recognizedTextView.getLineCount() - 1)
                        - recognizedTextView.getScrollY() - recognizedTextView.getHeight();
                if (scrollDelta > 0)
                    recognizedTextView.scrollBy(0, scrollDelta);
            }
        });

    }

    //endregion

    //region Direct Line Tasks

    /*
        Start Direct Line conversation with secret from preferences
     */
    private class CreateDirectLineConversationAsync extends AsyncTask<Void, Void, Void> {
        @Override
        protected Void doInBackground(Void... params) {
            try {
                directLineClient.StartConversation();
            } catch (Exception ex) {
                System.out.println(ex.getMessage());
            }
            return null;
        }

        @Override
        protected void onPostExecute(Void result) {
            mServerConnection = new WebSocketServerConnection(directLineClient.conversation.getStreamUrl());
            initializeServerConnection();
        }
    }

    /*
        Send required startup events to conversation
     */
    private class SendStartupEventsToDirectLineConversationAsync extends AsyncTask<Void, Void, Void> {
        @Override
        protected Void doInBackground(Void... params) {
            try {
                directLineClient.SendStartConversationEvent();
                directLineClient.SendVirtualAssistantTimeZoneEvent();
                directLineClient.SendVirtualAssistantLocationEvent(Configuration.Latitude, Configuration.Longitude);
            } catch (Exception ex) {
                System.out.println(ex.getMessage());
            }
            return null;
        }
    }

    //endregion

    /*
        Parse bot activities from websocket to configure speech, play, and update microphone setting
     */
    private void ParseBotActivitiesToSpeech(List<Activity> activities) throws IOException, JSONException, ParseException{
        for (Activity activity : activities) {
            String text = activity.getText();
            String speak = activity.getSpeak();
            ChannelAccount from = activity.getFrom();

            // 1. Activity.Speak
            // 2. Activity.Text
            // 3. Attachments: speak, title, text
            // 4. SuggestedActions.Text
            if(from.getId().equalsIgnoreCase(Configuration.BotId) && activity.getType() == ActivityTypes.MESSAGE) {
                StringBuilder speakText = new StringBuilder();
                if(speak != null && !speak.isEmpty()){
                    speakText.append(speak);
                }
                else if (text != null && !text.isEmpty()){
                    speakText.append(text);
                } else {
                    if (activity.getAttachments() != null && !activity.getAttachments().isEmpty()) {
                        StringBuilder attachmentText  = new StringBuilder();
                        for(int i = 0; i < activity.getAttachments().size(); i++){
                            Attachment attachment = activity.getAttachments().get(i);
                            switch (attachment.getContentType()) {
                                case "application/vnd.microsoft.card.hero":
                                    attachmentText.append(directLineClient.RenderHeroCard(attachment));
                                    break;
                                case "application/vnd.microsoft.card.adaptive":
                                    attachmentText.append(directLineClient.RenderAdaptiveCard(attachment));
                                    break;
                            }

                            if(activity.getAttachments().size() > 1){
                                // If there are multiple attachments, VA will pause between each one
                                attachmentText.append(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.ellipses));
                            }
                        }
                        speakText.append(attachmentText.toString());
                    }
                }

                activity.setSpeak(speakText.toString());
                new PlayActivitySpeakToSpeechAsync(activity).execute();
                speechHandler.new UpdateInputHintStatusAsync(activity.getInputHint()).execute();
            }
        }
    }



    private class PlayActivitySpeakToSpeechAsync extends AsyncTask<Void, Void, Void> {
        private Activity activity;

        public PlayActivitySpeakToSpeechAsync(Activity activityInput){
            activity = activityInput;
        }

        @Override
        protected Void doInBackground(Void... params) {
            List<String> playSpeakText = speechSynthesizer.splitSpeakText(activity.getSpeak());

            for(int i = 0; i < playSpeakText.size(); ++i){
                String speak = playSpeakText.get(i);
                if (i + 1 == playSpeakText.size()) {
                    // Because the player does not return anything, we
                    // will pass the action of updating UI to a runnable to be called after.
                    // Otherwise it displays messages before the bot is able to speak them all
                    Runnable runAfterFinalSpeak = new Runnable() {
                        @Override
                        public void run() {
                            MainActivity.this.runOnUiThread(() -> {
                                messageAdapter.add(activity);
                            });
                        }
                    };
                    speechSynthesizer.playText(speak, runAfterFinalSpeak);

                } else {
                    speechSynthesizer.playText(speak, null);
                }
            }
            return null;
        }
    }
}
