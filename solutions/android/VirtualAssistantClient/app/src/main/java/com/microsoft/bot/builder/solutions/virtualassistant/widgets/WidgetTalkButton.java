package com.microsoft.bot.builder.solutions.virtualassistant.widgets;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.util.Log;
import android.widget.RemoteViews;

import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.service.SpeechService;

import static com.microsoft.bot.builder.solutions.virtualassistant.service.SpeechService.ACTION_START_LISTENING;

/**
 * Implementation of App Widget functionality.
 * This Widget launches an activity to record audio
 */
public class WidgetTalkButton extends AppWidgetProvider {

    public static int WIDGET_LAYOUT = R.layout.widget_talk_button;

    /**
     * Updates all the widgets
     * @param context Context
     * @param appWidgetManager Widget Manager
     * @param appWidgetIds Widget IDs
     */
    @Override
    public void onUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
        Log.d("sender", "Broadcasting message");
        for (int appWidgetId : appWidgetIds) {
            RemoteViews remoteView = new RemoteViews(context.getPackageName(), WIDGET_LAYOUT);
            remoteView.setOnClickPendingIntent(R.id.push_to_talk, getPendingSelfIntent(context, appWidgetId));
            appWidgetManager.updateAppWidget(appWidgetId, remoteView);
        }

    }

    protected PendingIntent getPendingSelfIntent(Context context, int widgetId) {
        Intent intent = new Intent(context, SpeechService.class);
        intent.putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, widgetId);
        intent.setAction(ACTION_START_LISTENING);
        return PendingIntent.getService(context, 0, intent, 0);
    }

    @Override
    public void onEnabled(Context context) {
        // Enter relevant functionality for when the first widget is created
    }

    @Override
    public void onDisabled(Context context) {
        // Enter relevant functionality for when the last widget is disabled
    }
}

