package com.microsoft.bot.builder.solutions.eventcompanion

import android.app.Application
import android.content.IntentFilter

class MainApplication: Application() {
    private lateinit var eventReceiver: EventReceiver

    override fun onCreate() {
        super.onCreate()
        eventReceiver = EventReceiver()
        val filter = IntentFilter()
        filter.addAction(ACTION_BROADCAST)
        registerReceiver(eventReceiver, filter)
    }

    companion object {
        private const val ACTION_BROADCAST: String = "com.microsoft.broadcast"
    }
}
