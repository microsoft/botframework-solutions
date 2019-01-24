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
package io.swagger.client;

import android.os.Handler;
import android.os.Message;

import java.util.Arrays;
import java.util.concurrent.TimeUnit;
import okhttp3.ConnectionSpec;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.WebSocket;
import okhttp3.WebSocketListener;

public class WebSocketServerConnection {
    public enum ConnectionStatus {
        DISCONNECTED,
        CONNECTED
    }

    public interface ServerListener {
        void onNewMessage(String message);
        void onStatusChange(ConnectionStatus status);
    }

    private WebSocket mWebSocket;
    private OkHttpClient mClient;
    private String mServerUrl;
    private Handler mMessageHandler;
    private Handler mStatusHandler;
    private ServerListener mListener;


    private class SocketListener extends WebSocketListener {
        @Override
        public void onOpen(WebSocket webSocket, Response response) {
            Message m = mStatusHandler.obtainMessage(0, ConnectionStatus.CONNECTED);
            mStatusHandler.sendMessage(m);
        }

        @Override
        public void onMessage(WebSocket webSocket, String text) {
            Message m = mMessageHandler.obtainMessage(0, text);
            mMessageHandler.sendMessage(m);
        }

        @Override
        public void onClosed(WebSocket webSocket, int code, String reason) {
            Message m = mStatusHandler.obtainMessage(0, ConnectionStatus.DISCONNECTED);
            mStatusHandler.sendMessage(m);
        }

        @Override
        public void onFailure(WebSocket webSocket, Throwable t, Response response) {
            Disconnect();
        }
    }

    public WebSocketServerConnection(String url) {
        mClient = new OkHttpClient.Builder()
                .connectionSpecs(Arrays.asList(ConnectionSpec.MODERN_TLS, ConnectionSpec.COMPATIBLE_TLS))
                .readTimeout(3,  TimeUnit.SECONDS)
                .retryOnConnectionFailure(true)
                .build();
        mServerUrl = url;
    }

    public void Connect(ServerListener listener) {
        Request request = new Request.Builder()
                .url(mServerUrl)
                .build();
        mWebSocket = mClient.newWebSocket(request, new SocketListener());
        mListener = listener;
        mMessageHandler = new Handler(msg -> {mListener.onNewMessage((String) msg.obj);
            return true;});
        mStatusHandler = new Handler(msg -> { mListener.onStatusChange((ConnectionStatus) msg.obj);
            return true;});
    }

    public void Disconnect() {
        mWebSocket.cancel();
        mListener = null;
        mMessageHandler.removeCallbacksAndMessages(null);
        mStatusHandler.removeCallbacksAndMessages(null);
    }
}
