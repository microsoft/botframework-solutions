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

import com.fasterxml.jackson.databind.ObjectMapper;

import org.json.simple.JSONObject;
import org.json.simple.parser.JSONParser;
import org.json.simple.parser.ParseException;
import org.threeten.bp.OffsetDateTime;

import java.io.IOException;
import java.util.TimeZone;

import io.swagger.client.ApiClient;
import io.swagger.client.ApiException;
import io.swagger.client.api.ConversationsApi;
import io.swagger.client.model.Activity;
import io.swagger.client.model.ActivityTypes;
import io.swagger.client.model.Attachment;
import io.swagger.client.model.ChannelAccount;
import io.swagger.client.model.Conversation;
import io.swagger.client.model.ResourceResponse;

public class BotApplication{
    private static final String DirectLineConstant = "directline";
    private static final ChannelAccount user = new ChannelAccount();

    public static class DirectLineClient {

        private ConversationsApi conversationsApi = new ConversationsApi();
        private ApiClient client = conversationsApi.getApiClient();
        public Conversation conversation = null;

        /*
         * Calls Direct Line API to start a conversation and saves the conversation object in DirectLineClient
         */
        public void StartConversation() throws ApiException {
            client.addDefaultHeader("Authorization", "Bearer " + Configuration.DirectLineSecret);

            // Enable Jersey LoggingFilter and you can check contents of requests
            client.setDebugging(true);

            user.setName(Configuration.UserName);
            user.setId(Configuration.UserId);

            System.out.println("@@conversation start");
            conversation = conversationsApi.conversationsStartConversation();
        }

        /*
         * Create Event Activity with inputs: name, channel data, and value
         */
        public Activity CreateEventActivity(String name, Object channelData, Object value) throws ApiException {
            Activity activity = new Activity();
            activity.setType(ActivityTypes.EVENT);
            activity.setLocale(Configuration.Locale);
            activity.setFrom(user);
            activity.setChannelId(DirectLineConstant);
            activity.setChannelData(channelData);
            activity.setName(name);
            activity.setValue(value);

            return activity;
        }

        /*
         * Create a Message Activity with inputs: text and channel data
         */
        public Activity CreateMessageActivityFromUser(String text, Object channelData) {
            System.out.println("@@post a conversation message");
            Activity activity = new Activity();
            activity.setType(ActivityTypes.MESSAGE);
            activity.setLocale(Configuration.Locale);
            activity.setFrom(user);
            activity.setChannelId(DirectLineConstant);
            activity.setText(text);
            activity.setChannelData(channelData);
            activity.setTimestamp(OffsetDateTime.now());
            activity.setSpeak(text);

            return activity;
        }

        /*
            Post Activity to Direct Line API
         */
        public void SendActivity(Activity activity) throws ApiException {
            ResourceResponse response = conversationsApi.conversationsPostActivity(conversation.getConversationId(), activity);
        }

        /*
        Send startConversation event, with locale, to the bot
         */
        public void SendStartConversationEvent() throws ApiException {
            Activity eventActivity = CreateEventActivity(Configuration.StartConversationEvent, null, null);
            SendActivity(eventActivity);
        }

        /*
         * Send the IPA.Location event to the bot
         */
        public void SendVirtualAssistantLocationEvent(String latitude, String longitude) throws ApiException {
            String coordinates = latitude + "," + longitude;
            Activity eventActivity = CreateEventActivity(Configuration.IPALocationEvent, null, coordinates);
            SendActivity(eventActivity);
        }

        /*
         * Send the IPA.TimeZone event to the bot
         */
        public void SendVirtualAssistantTimeZoneEvent() throws ApiException {
            TimeZone tz = TimeZone.getDefault();
            Activity eventActivity = CreateEventActivity(Configuration.IPATimezoneEvent, null, tz.getDisplayName());
            SendActivity(eventActivity);

        }

        /*
         * Parse Adaptive Cards into JSON strings and extract the speak property
         */
        public String RenderAdaptiveCard(Attachment attachment) throws IOException, ParseException {
            ObjectMapper mapperObj = new ObjectMapper();
            JSONParser parser = new JSONParser();

            String jsonStr = mapperObj.writeValueAsString(attachment.getContent());
            JSONObject json = (JSONObject) parser.parse(jsonStr);
            String speak = (String) json.get("speak");
            return speak;
        }

        /*
         * Parse Hero Cards into JSON strings and extract the title, subtitle, text properties
         */
        public String RenderHeroCard(Attachment attachment) throws IOException, ParseException {
            ObjectMapper mapperObj = new ObjectMapper();
            JSONParser parser = new JSONParser();
            String jsonStr = mapperObj.writeValueAsString(attachment.getContent());
            JSONObject json = (JSONObject) parser.parse(jsonStr);
            String title = (String) json.get("title");
            String subtitle = (String) json.get("subtitle");
            String text = (String) json.get("text");
            String response = "";

            if (title != null) {
                response += title + "...";
            }

            if (subtitle != null) {
                response += subtitle + "...";
            }

            if (text != null) {
                response += text + "...";
            }

            return response;
        }
    }
}