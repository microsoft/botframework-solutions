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

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import com.microsoft.speech.tts.Voice;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.util.HashMap;

import io.swagger.client.model.InputHints;

public final  class Configuration {
    // Device & SDK
    public static String SpeechSubscriptionKey = "";
    public static String SpeechRegion = "";
    public static String Keyword = "";
    public static final String KeywordModel = "/data/keyword/kws.table";
    public static String DeviceGeometry = "";
    public static String SelectedGeometry = "";
    public static String AccessTokenUri = "";
    public static String TextToSpeechServiceUri = "";
    public static String TextToSpeechKey = "";

    // Voices
    // Find additional voices at: https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#text-to-speech
    private static String EnglishMaleVoice = "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)";
    private static String EnglishFemaleVoice = "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)";
    private static String GermanMaleVoice = "Microsoft Server Speech Text to Speech Voice (de-DE, Stefan, Apollo)";
    private static String GermanFemaleVoice = "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)";
    private static String FrenchMaleVoice = "Microsoft Server Speech Text to Speech Voice (fr-FR, Paul, Apollo)";
    private static String FrenchFemaleVoice = "Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)";
    private static String SpanishMaleVoice = "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)";
    private static String SpanishFemaleVoice = "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)";
    private static String ItalianMaleVoice = "Microsoft Server Speech Text to Speech Voice (it-IT, Cosimo, Apollo)";
    private static String ItalianFemaleVoice = "Microsoft Server Speech Text to Speech Voice (it-IT, LuciaRUS)";
    private static String ChineseMaleVoice = "Microsoft Server Speech Text to Speech Voice (zh-CN, Kangkang, Apollo)";
    private static String ChineseFemaleVoice = "Microsoft Server Speech Text to Speech Voice (zh-CN, Yaoyao, Apollo)";

    public static String NeuralTextToSpeechKey = "";
    public static String NeuralMaleTextToSpeechServiceUri = "";
    public static String NeuralFemaleTextToSpeechServiceUri = "";
    public static String NeuralAccessTokenUri = "";
    private static String EnglishNeuralMaleVoice = "Microsoft Server Speech Text to Speech Voice (en-US, GuyNeural)";
    private static String EnglishNeuralFemaleVoice = "Microsoft Server Speech Text to Speech Voice (en-US, JessaNeural)";

    //Bot & Conversation
    public static String UserName = "User";
    public static String UserId = "";
    public static String Locale = "";
    public static String DirectLineSecret = "";
    public static InputHints DefaultInputHint;

    // IPA.TimeZone event name for Virtual Assistant
    public static String IPATimezoneEvent = "IPA.Timezone";
    // IPA.Location event name for Virtual Assistant
    public static String IPALocationEvent = "IPA.Location";
    // startConversation event
    public static String StartConversationEvent = "startConversation";
    public static String Latitude = "";
    public static String Longitude = "";

    // Speech settings that are set in /res/values/configuration.xml
    public static String VoiceName = "";
    public static Voice.Gender Gender = null;
    public static Boolean UseNeuralTTS = false;
    public static String BotId = "";



    //Compare configuration values to preferences settings and return true if changed
    public static boolean CheckForUpdatedPreferences(SharedPreferences preferences){
        if(DirectLineSecret != preferences.getString("directlinesecret","")){
            return true;
        } else if(BotId != preferences.getString("botid","")){
            return true;
        } else if(UserId != preferences.getString("userid","")){
            return true;
        } else if(Locale != preferences.getString("locale","")){
            return true;
        } else if(DeviceGeometry != preferences.getString("devicegeometry","")){
            return true;
        } else if(SelectedGeometry != preferences.getString("selectedgeometry","")){
            return true;
        } else if(Gender != Voice.Gender.valueOf(preferences.getString("gender", ""))){
            return true;
        } else if(UseNeuralTTS != preferences.getBoolean("neural", true)){
            return true;
        } else if(DefaultInputHint != InputHints.valueOf(preferences.getString("defaultinputhint", "").toUpperCase())){
            return true;
        }

        return false;
    }


    //The latitude/longitude settings will be checked separately to enable sending
    //an updated location event to the bot.
    public static boolean CheckForUpdatedCoordinates(SharedPreferences preferences){
        if(Latitude != preferences.getString("latitude","")){
            return true;
        } else if(Longitude != preferences.getString("longitude","")){
            return true;
        }

        return false;
    }

    //Save shared preferences to configuration
    public static void UpdateConfigurationWithSharedPreferences(Context context, SharedPreferences preferences){
        SpeechSubscriptionKey = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.speechsubscriptionkey);
        SpeechRegion = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.speechregion);
        Keyword = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.keyword);
        AccessTokenUri = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.accesstokenuri);
        TextToSpeechServiceUri = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.texttospeechserviceuri);
        TextToSpeechKey = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.texttospeechkey);

        NeuralTextToSpeechKey = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.neuraltexttospeechkey);
        NeuralMaleTextToSpeechServiceUri = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.neuralmaletexttospeechserviceuri);
        NeuralFemaleTextToSpeechServiceUri = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.neuralfemaletexttospeechserviceuri);
        NeuralAccessTokenUri = context.getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.neuralaccesstokenuri);

        Latitude = preferences.getString("latitude", "");
        Longitude = preferences.getString("longitude", "");
        UserId = preferences.getString("userid", "");
        DirectLineSecret = preferences.getString("directlinesecret","");
        BotId = preferences.getString("botid","");
        Locale = preferences.getString("locale","");
        DefaultInputHint = InputHints.valueOf(preferences.getString("defaultinputhint", "").toUpperCase());
        Gender = Voice.Gender.valueOf(preferences.getString("gender", ""));
        DeviceGeometry = preferences.getString("devicegeometry","");
        SelectedGeometry = preferences.getString("selectedgeometry","");
        UseNeuralTTS = preferences.getBoolean("neural", true);
        ConfigureVoice();
    }

    public static boolean CheckForMissingConfiguration(){
        if(SpeechSubscriptionKey.equals("")){
            return true;
        } else if(SpeechRegion.equals("")){
            return true;
        } else if(Keyword.equals("")){
            return true;
        } else if(AccessTokenUri.equals("")){
            return true;
        } else if(TextToSpeechServiceUri.equals("")){
            return true;
        } else if(TextToSpeechKey.equals("")){
            return true;
        } else if(NeuralTextToSpeechKey.equals("")){
            return true;
        } else if(NeuralMaleTextToSpeechServiceUri.equals("")){
            return true;
        } else if(NeuralFemaleTextToSpeechServiceUri.equals("")){
            return true;
        } else if(NeuralAccessTokenUri.equals("")){
            return true;
        }

        return false;
    }

    public static void ConfigureVoice(){
        if(Locale.equalsIgnoreCase("en-us")){
            if(Gender.equals(Voice.Gender.Male)){
                if(UseNeuralTTS){
                    AccessTokenUri = NeuralAccessTokenUri;
                    TextToSpeechServiceUri =  NeuralMaleTextToSpeechServiceUri;
                    TextToSpeechKey = NeuralTextToSpeechKey;
                    VoiceName = EnglishNeuralMaleVoice;
                } else {
                    VoiceName = EnglishMaleVoice;
                }
            } else if (Gender.equals(Voice.Gender.Female)){
                if(UseNeuralTTS){
                    AccessTokenUri = NeuralAccessTokenUri;
                    TextToSpeechServiceUri = NeuralFemaleTextToSpeechServiceUri;
                    TextToSpeechKey = NeuralTextToSpeechKey;
                    VoiceName = EnglishNeuralFemaleVoice;
                } else {
                    VoiceName = EnglishFemaleVoice;
                }
            }
        } else if(Locale.equalsIgnoreCase("de-de")){
            if(Gender.equals(Voice.Gender.Male)) {
                VoiceName = GermanMaleVoice;
            } else if (Gender.equals(Voice.Gender.Female)){
                VoiceName = GermanFemaleVoice;
            }
        } else if(Locale.equalsIgnoreCase("es-es")){
            if(Gender.equals(Voice.Gender.Male)) {
                VoiceName = SpanishMaleVoice;
            } else if (Gender.equals(Voice.Gender.Female)){
                VoiceName = SpanishFemaleVoice;
            }
        }  if(Locale.equalsIgnoreCase("fr-fr")){
            if(Gender.equals(Voice.Gender.Male)) {
                VoiceName = FrenchMaleVoice;
            } else if (Gender.equals(Voice.Gender.Female)){
                VoiceName = FrenchFemaleVoice;
            }
        } else if(Locale.equalsIgnoreCase("it-it")){
            if(Gender.equals(Voice.Gender.Male)) {
                VoiceName = ItalianMaleVoice;
            } else if (Gender.equals(Voice.Gender.Female)){
                VoiceName = ItalianFemaleVoice;
            }
        } else if(Locale.equalsIgnoreCase("zh-cn")){
            if(Gender.equals(Voice.Gender.Male)) {
                VoiceName = ChineseMaleVoice;
            } else if (Gender.equals(Voice.Gender.Female)){
                VoiceName = ChineseFemaleVoice;
            }
        }

    }
}
