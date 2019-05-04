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

import com.microsoft.cognitiveservices.speech.texttospeech.Synthesizer;
import com.microsoft.speech.tts.Voice;
import java.util.ArrayList;
import java.util.List;
import java.util.StringTokenizer;

public class SpeechSynthesizer {

    private Synthesizer synthesizer;

    public SpeechSynthesizer() {
        synthesizer = new Synthesizer(Configuration.TextToSpeechKey);
        Voice voice = new Voice(Configuration.Locale, Configuration.VoiceName, Configuration.Gender, true);
        synthesizer.SetVoice(voice, null);
    }

    /*
    Play text as speech with the configured Voice
     */
    public void playText(final String text, final Runnable callbackfunc){
        synthesizer.SpeakToAudio(text, callbackfunc);
    }

    /*
    Split a string into segments of 200 characters to be sent to the synthesizer to avoid latency
     */
    public List<String> splitSpeakText(String speakText){
        int maxCharCount = 200;
        List<String> outputList = new ArrayList<>();

        if(speakText.length() < maxCharCount){
            outputList.add(speakText);
            return outputList;
        }

        StringTokenizer tok = new StringTokenizer(speakText, " ");
        StringBuilder output = new StringBuilder(speakText.length());

        while(tok.hasMoreTokens()){
            String word = tok.nextToken();
            if(output.length() + word.length() > maxCharCount){
                outputList.add(output.toString());
                output.setLength(0);
            }

            output.append(word + " ");

            if(!tok.hasMoreTokens()){
                outputList.add(output.toString());
            }
        }

        return outputList;
    }
}
