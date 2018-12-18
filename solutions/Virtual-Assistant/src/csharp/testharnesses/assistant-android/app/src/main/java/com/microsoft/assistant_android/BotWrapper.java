package com.microsoft.assistant_android;

import org.jetbrains.annotations.NotNull;
import org.json.JSONObject;

import android.os.StrictMode;
import android.util.Log;

import com.microsoft.directlinechatbot.DirectLineChatbot;
import com.microsoft.directlinechatbot.bo.ChannelData;
import com.microsoft.directlinechatbot.bo.Claims;
import com.microsoft.directlinechatbot.bo.GeoLocation;

public class BotWrapper {
    private static String TAG = "Bot";
    private String primaryToken;
    private DirectLineChatbot _chatbot = null;
    private ChannelData _channleData = null;
    private BotEventInterface _botEventInterface;

    public String botName = "gmipapoc-botchannel";

    public int BotState = -1;
    public int BOT_STATE_ACCEPTING_INPUT = 1;
    public int BOT_STATE_IGNORING_INPUT = 2;
    public int BOT_STATE_EXPECTING_INPUT = 3;

    public BotWrapper(BotEventInterface event, String botToken) {
        _botEventInterface = event;
        primaryToken = botToken;
        PopulateUserInfo();
        startConversation();
    }

    public void SendMessage(String text) {
        _chatbot.send("","message", text, _channleData);
    }

    public void EndConversation() {
        if (_chatbot != null) {
            _chatbot.send("", "endOfConversation", "", _channleData);
        }
    }

    //returns the conversationID
    private void startConversation() {
        Log.d(TAG,"Bot Secret: " + primaryToken);
        _chatbot = new DirectLineChatbot(primaryToken);
        _chatbot.setDebug(true);
        _chatbot.setUser("MeganB@GMIPATest.OnMicrosoft.com");

        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();

        StrictMode.setThreadPolicy(policy);
        _chatbot.start(new DirectLineChatbot.Callback()
        {
            @Override
            public void onStarted()
            {
                _chatbot.send("OpenAssistant","event", "", _channleData);
            }

            @Override
            public void onMessageReceived(@NotNull String message)
            {
                try {
                    JSONObject json = new JSONObject(message);
                    String errorText = json.optString("text",null);
                    if (errorText != null)
                        _botEventInterface.onBotError(errorText);
                    else {
                        //Log.d("CHATBOT RECV: ", message);
                        _botEventInterface.onMessageReceived(message);
                    }
                }
                catch (Exception e) {
                    e.printStackTrace();
                    _botEventInterface.onBotError("Error in BotWrapper::onMessageReceived. Error: " + e.getMessage());
                }
            }
        });
        _botEventInterface.onBotReady();
    }

    private void PopulateUserInfo() {
        GeoLocation geo = new GeoLocation(47.7069312,-122.09520640000001);
        Claims claims = new Claims("true");
        _channleData = new ChannelData("MeganVIN101",
                "MeganB@GMIPATest.OnMicrosoft.com",
                false,
                "Megan",
                "Bowen",
                geo,
                claims);
    }

}