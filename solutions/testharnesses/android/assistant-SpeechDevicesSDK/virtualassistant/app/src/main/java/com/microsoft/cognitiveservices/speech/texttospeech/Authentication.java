//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-Speech-TTS
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
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
//
package com.microsoft.cognitiveservices.speech.texttospeech;

import android.util.Log;
import com.microsoft.bot.solutions.speechdevices.samples.botapp.Configuration;
import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.net.URL;
import java.util.Timer;
import java.util.TimerTask;

import javax.net.ssl.HttpsURLConnection;

/*
     * This class demonstrates how to get a valid O-auth accessToken from
     * Azure Data Market.
     */
class Authentication {
    private static final String LOG_TAG = "Authentication";
    private String apiKey;
    private String accessToken;
    private Timer accessTokenRenewer;

    // Access Token expires every 10 minutes. Renew it every 9 minutes only.
    private final int RefreshTokenDuration = 9 * 60 * 1000;
    private TimerTask nineMinitesTask = null;

    public Authentication(String apiKey) {
        this.apiKey = apiKey;

        Thread th = new Thread(new Runnable() {
            @Override
            public void run() {
                RenewAccessToken();
            }
        });

        try {
            th.start();
            th.join();
        } catch (Exception e) {
            e.printStackTrace();
        }

        // renew the accessToken every specified minutes
        accessTokenRenewer = new Timer();
        nineMinitesTask = new TimerTask() {
            public void run() {
                RenewAccessToken();
            }
        };

        accessTokenRenewer.schedule(nineMinitesTask, RefreshTokenDuration, RefreshTokenDuration);
    }

    public String GetAccessToken() {
        return this.accessToken;
    }

    private void RenewAccessToken() {
        synchronized(this) {
            HttpPost(Configuration.AccessTokenUri, this.apiKey);

            if(this.accessToken != null){
                Log.d(LOG_TAG, "new Access Token: " + this.accessToken);
            }
        }
    }

    private void HttpPost(String AccessTokenUri, String apiKey) {
        InputStream inSt = null;
        HttpsURLConnection webRequest = null;

        this.accessToken = null;
        //Prepare OAuth request
        try{
            URL url = new URL(AccessTokenUri);
            webRequest = (HttpsURLConnection) url.openConnection();
            webRequest.setDoInput(true);
            webRequest.setDoOutput(true);
            webRequest.setConnectTimeout(5000);
            webRequest.setReadTimeout(5000);
            webRequest.setRequestProperty("Ocp-Apim-Subscription-Key", apiKey);
            webRequest.setRequestMethod("POST");

            String request = "";
            byte[] bytes = request.getBytes();
            webRequest.setRequestProperty("content-length", String.valueOf(bytes.length));
            webRequest.connect();

            DataOutputStream dop = new DataOutputStream(webRequest.getOutputStream());
            dop.write(bytes);
            dop.flush();
            dop.close();

            inSt = webRequest.getInputStream();
            InputStreamReader in = new InputStreamReader(inSt);
            BufferedReader bufferedReader = new BufferedReader(in);
            StringBuffer strBuffer = new StringBuffer();
            String line = null;
            while ((line = bufferedReader.readLine()) != null) {
                strBuffer.append(line);
            }

            bufferedReader.close();
            in.close();
            inSt.close();
            webRequest.disconnect();

            this.accessToken = strBuffer.toString();
            Log.e(LOG_TAG, "HttpPost: TOKEN:"+strBuffer.toString());

        } catch (Exception e) {
            Log.e(LOG_TAG, "Exception error", e);
        }
    }
}
