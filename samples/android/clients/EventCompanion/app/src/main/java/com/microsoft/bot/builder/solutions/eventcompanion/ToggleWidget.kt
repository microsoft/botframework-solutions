package com.microsoft.bot.builder.solutions.eventcompanion

import android.app.PendingIntent
import android.appwidget.AppWidgetManager
import android.appwidget.AppWidgetProvider
import android.content.Context
import android.content.Intent
import android.util.Log
import android.widget.RemoteViews
import com.squareup.picasso.Picasso

/**
 * Implementation of App Widget functionality.
 * App Widget Configuration implemented in [ToggleWidgetConfigureActivity]
 */
class ToggleWidget : AppWidgetProvider() {

    override fun onUpdate(context: Context, appWidgetManager: AppWidgetManager, appWidgetIds: IntArray) {
        // There may be multiple widgets active, so update all of them
        for (appWidgetId in appWidgetIds) {
            updateAppWidget(context, appWidgetManager, appWidgetId)
        }
    }

    override fun onDeleted(context: Context, appWidgetIds: IntArray) {
        // When the user deletes the widget, delete the preference associated with it.
        for (appWidgetId in appWidgetIds) {
            ToggleWidgetConfigureActivity.removeData(context, appWidgetId)
            ToggleWidgetConfigureActivity.removeConf(context, appWidgetId)
        }
    }

    override fun onEnabled(context: Context) {
        // Enter relevant functionality for when the first widget is created
    }

    override fun onDisabled(context: Context) {
        // Enter relevant functionality for when the last widget is disabled
    }

    override fun onReceive(context: Context, intent: Intent) {
        if(intent.action == ACTION_CLICK) {
            val appWidgetId = intent.getIntExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, AppWidgetManager.INVALID_APPWIDGET_ID)
            val widgetData = ToggleWidgetConfigureActivity.loadData(context, appWidgetId)
            widgetData.value = !widgetData.value
            ToggleWidgetConfigureActivity.saveData(context, appWidgetId, widgetData)
            val views = RemoteViews(context.packageName, R.layout.toggle_widget)
            val toggleColor = if(widgetData.value) context.getColor(R.color.color_toggle_on) else context.getColor(R.color.color_toggle_off)
            views.setInt(R.id.widget_indicator, "setColorFilter", toggleColor)
            val appWidgetManager = AppWidgetManager.getInstance(context)
            appWidgetManager.updateAppWidget(appWidgetId, views)
        }
        Log.d("intent action", intent.action.toString())
        super.onReceive(context, intent)
    }

    companion object {

        internal fun updateAppWidget(
            context: Context, appWidgetManager: AppWidgetManager,
            appWidgetId: Int
        ) {
            // Construct the RemoteViews object
            val views = RemoteViews(context.packageName, R.layout.toggle_widget)
            views.setOnClickPendingIntent(R.id.widget_toggle, getPendingSelfIntent(context, ACTION_CLICK, appWidgetId))

            val widgetConf = ToggleWidgetConfigureActivity.loadConf(context, appWidgetId)
            val widgetData = ToggleWidgetConfigureActivity.loadData(context, appWidgetId)
            views.setTextViewText(R.id.widget_label, widgetConf.label)
            if (widgetConf.icon.isNotEmpty()) {
                Picasso.get().load(widgetConf.icon).into(views, R.id.widget_icon, intArrayOf(appWidgetId))
            }
            val toggleColor = if(widgetData.value) context.getColor(R.color.color_toggle_on) else context.getColor(R.color.color_toggle_off)
            views.setInt(R.id.widget_indicator, "setColorFilter", toggleColor)

            // Instruct the widget manager to update the widget
            appWidgetManager.updateAppWidget(appWidgetId, views)
        }

        private fun getPendingSelfIntent(context: Context, action: String, appWidgetId: Int): PendingIntent {
            val intent = Intent(context, ToggleWidget::class.java)
            intent.action = action
            intent.putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, appWidgetId)
            return PendingIntent.getBroadcast(context, appWidgetId, intent, PendingIntent.FLAG_UPDATE_CURRENT)
        }

        private const val ACTION_CLICK: String = "com.microsoft.bot.builder.solutions.eventcompanion.widgets.click"
    }
}

