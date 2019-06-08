package com.microsoft.bot.builder.solutions.virtualassistant.service;

import android.os.Binder;

public class ServiceBinder extends Binder {

    private SpeechService speechService;

    public ServiceBinder(SpeechService speechService) {
        this.speechService = speechService;
    }

    public SpeechService getSpeechService() {
        return speechService;
    }
}