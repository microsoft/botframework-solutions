package com.microsoft.bot.builder.solutions.eventcompanion

import android.appwidget.AppWidgetManager
import android.content.BroadcastReceiver
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import com.google.gson.Gson

class EventReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, receivedIntent: Intent) {
        // This method is called when the BroadcastReceiver is receiving an Intent broadcast.
        if (receivedIntent.hasExtra("WidgetUpdate")) {
            val activityString = receivedIntent.getStringExtra("WidgetUpdate")
            val botActivity = Gson().fromJson(activityString, BotActivity::class.java)
            if (botActivity.type == "event") {
                val eventName = botActivity.name
                val appWidgetManager = AppWidgetManager.getInstance(context)
                val toggleWidgetIds = appWidgetManager.getAppWidgetIds(ComponentName(context, ToggleWidget::class.java))
                for (id in toggleWidgetIds) {
                    val widgetConf = ToggleWidgetConfigureActivity.loadConf(context, id)
                    if (eventName == widgetConf.event) {
                        val widgetData = ToggleWidgetConfigureActivity.loadData(context, id)
                        when (botActivity.getValue()) {
                            "On" -> widgetData.value = true
                            "Off" -> widgetData.value = false
                        }
                        ToggleWidgetConfigureActivity.saveData(context, id, widgetData)
                        ToggleWidget.updateAppWidget(context, appWidgetManager, id)
                    }
                }
                val numericWidgetIds = appWidgetManager.getAppWidgetIds(ComponentName(context, NumericWidget::class.java))
                for (id in numericWidgetIds) {
                    val widgetConf = NumericWidgetConfigureActivity.loadConf(context, id)
                    if (eventName == widgetConf.event) {
                        val eventValue = botActivity.getValue()
                        val eventAmount = botActivity.getAmount()
                        val eventUnit = botActivity.getUnit()
                        val widgetData = NumericWidgetConfigureActivity.loadData(context, id)
                        when (eventValue) {
                            "Increase" -> widgetData.value += if (eventAmount != 0F) {
                                eventAmount
                            } else {
                                1F//TODO: make constant
                            }
                            "Decrease" -> widgetData.value += if (eventAmount != 0F) {
                                eventAmount
                            } else {
                                -1F//TODO: make constant
                            }
                            "Set" -> widgetData.value = eventAmount
                        }
                        for (range in widgetConf.ranges) {
                            if (range.unit == eventUnit) {
                                if (widgetData.value < range.minimum) {
                                    widgetData.value = range.minimum
                                } else if (widgetData.value > range.maximum) {
                                    widgetData.value = range.maximum
                                }
                                widgetData.unit = range.unit
                                break
                            }
                        }
                        NumericWidgetConfigureActivity.saveData(context, id, widgetData)
                        NumericWidget.updateAppWidget(context, appWidgetManager, id)
                    }
                }
            }
        }
    }

    private data class Value(val value: String?, val amount: Amount?)
    private data class Amount(val amount: Float?, val unit: String?)
    private data class BotActivity(val name: String, val type: String, val value: Value?) {
        fun getValue(): String {
            return value?.value?: ""
        }
        fun getAmount(): Float {
            return value?.amount?.amount?: 0F
        }
        fun getUnit(): String {
            return value?.amount?.unit?: ""
        }
    }
}


