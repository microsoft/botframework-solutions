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
package com.microsoft.bot.builder.solutions.directlinespeech;

import android.media.AudioAttributes;
import android.media.AudioFormat;
import android.media.AudioTrack;
import android.util.Log;

import com.microsoft.cognitiveservices.speech.audio.PullAudioInputStream;
import com.microsoft.cognitiveservices.speech.audio.PullAudioOutputStream;

import java.io.IOException;
import java.io.PipedInputStream;
import java.io.PipedOutputStream;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;
import java.util.ListIterator;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class Synthesizer {

    static final String logTag = "Synthesizer";

    private AudioTrack audioTrack;
    final int SAMPLE_RATE = 16000;
    static final int channelConfiguration = AudioFormat.CHANNEL_OUT_MONO;
    static final int audioEncoding = AudioFormat.ENCODING_PCM_16BIT;
    private AtomicBoolean isPlaying = new AtomicBoolean(false);
    private int playBufSize;
    private LinkedList<PullAudioOutputStream> streamList ;

    private Lock streamListLock = new ReentrantLock();


    public boolean IsPlaying()
    {
        return isPlaying.get();
    }

    public void stopPlaying()
    {
        isPlaying.set(false);
    }
    public void playStream(PipedOutputStream out, final Runnable callback)
    {
        try {
            playBufSize = AudioTrack.getMinBufferSize(SAMPLE_RATE, channelConfiguration, audioEncoding);
            PipedInputStream in = new PipedInputStream(out, playBufSize);

            AudioAttributes attrs = new AudioAttributes.Builder().
                    setContentType(AudioAttributes.CONTENT_TYPE_SPEECH).
                    setUsage(AudioAttributes.USAGE_MEDIA).build();

            AudioFormat fmt = new AudioFormat.Builder().
                    setChannelMask(AudioFormat.CHANNEL_OUT_MONO).
                    setEncoding(AudioFormat.ENCODING_PCM_16BIT).
                    setSampleRate(SAMPLE_RATE).build();
            audioTrack = new AudioTrack(attrs, fmt, playBufSize, AudioTrack.MODE_STREAM, 0);
            new Thread() {
                byte[] buffer = new byte[playBufSize];

                public void run() {
                    audioTrack.play();
                    isPlaying.set(true);
                    long readSize = -1;

                    while (isPlaying.get()) {
                        try {
                            if(streamList.peekFirst() != null){
                                PullAudioOutputStream stream = streamList.getFirst();
                                while(readSize != 0){
                                    readSize = stream.read(buffer);
                                    audioTrack.write(buffer, 0, (int)readSize);
                                }
                                streamList.removeFirst();
                            }
                            else{
                                Thread.sleep(50);
                            }

                            //readSize = in.read(buffer);
                        } catch (Exception e) {
                            Log.e(logTag, "read exception", e);
                            break;
                        }
                        //audioTrack.write(buffer, 0, readSize);
                    }
                    audioTrack.stop();
                    try {
                        in.close();
                        callback.run();
                    } catch (Exception e) {
                        Log.e(logTag, "close exception", e);
                    }
                }
            }.start();
        }
        catch (IOException e)
        {
            Log.e(logTag, "init exception", e);
        }
    }

    //stop playing audio data
    // if use STREAM mode, will wait for the end of the last write buffer data will stop.
    // if you stop immediately, call the pause() method and then call the flush() method to discard the data that has not yet been played
    public void stopSound() {
        try {
            if (audioTrack != null && audioTrack.getState() == AudioTrack.STATE_INITIALIZED) {
                audioTrack.pause();
                audioTrack.flush();

            }
        } catch (Exception e) {
            Log.e(logTag, "StopSound", e);
        }
    }

    public Synthesizer() {
        this.streamList = new LinkedList<PullAudioOutputStream>();
    }

    public void playStream(PullAudioOutputStream stream){
        if(!isPlaying.get()){
            safeAddToStreamList(stream);
            startPlaying();
        }
        else {
            safeAddToStreamList(stream);
        }
    }

    private void safeAddToStreamList(PullAudioOutputStream stream){
        this.streamListLock.lock();
        try {
            this.streamList.add(stream);
        } finally {
            this.streamListLock.unlock();
        }
    }

    private void startPlaying(){
            isPlaying.set(true);
            playBufSize = AudioTrack.getMinBufferSize(SAMPLE_RATE, channelConfiguration, audioEncoding);

            AudioAttributes attrs = new AudioAttributes.Builder().
                    setContentType(AudioAttributes.CONTENT_TYPE_SPEECH).
                    setUsage(AudioAttributes.USAGE_MEDIA).build();

            AudioFormat fmt = new AudioFormat.Builder().
                    setChannelMask(AudioFormat.CHANNEL_OUT_MONO).
                    setEncoding(AudioFormat.ENCODING_PCM_16BIT).
                    setSampleRate(SAMPLE_RATE).build();
            audioTrack = new AudioTrack(attrs, fmt, playBufSize, AudioTrack.MODE_STREAM, 0);
            new Thread() {
                byte[] buffer = new byte[playBufSize];

                public void run() {
                    audioTrack.play();
                    isPlaying.set(true);
                    long readSize = -1;

                    while (streamList.size() > 0) {
                        try {
                            if(streamList.peekFirst() != null){
                                PullAudioOutputStream stream = streamList.getFirst();
                                while(readSize != 0){
                                    readSize = stream.read(buffer);
                                    audioTrack.write(buffer, 0, (int)readSize);
                                }

                                streamListLock.lock();
                                try{
                                    streamList.removeFirst();
                                }
                                finally {
                                    streamListLock.unlock();
                                }
                            }

                            //readSize = in.read(buffer);
                        } catch (Exception e) {
                            Log.e(logTag, "read exception", e);
                            break;
                        }
                        //audioTrack.write(buffer, 0, readSize);
                    }
                    audioTrack.stop();
                    isPlaying.set(false);
                }
            }.start();
    }


}
