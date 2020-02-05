package com.microsoft.bot.builder.solutions.eventcompanion

import android.app.Activity
import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView

class MainActivity : AppCompatActivity() {

    private var widgetConfList: ArrayList<WidgetConf> = ArrayList()
    private lateinit var recyclerView: RecyclerView
    private lateinit var viewAdapter: RecyclerView.Adapter<*>
    private lateinit var viewManager: RecyclerView.LayoutManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        viewManager = LinearLayoutManager(this)
        viewAdapter = WidgetListAdapter(this, widgetConfList)
        recyclerView = findViewById(R.id.widget_list)
        recyclerView.apply {
            setHasFixedSize(true)
            layoutManager = viewManager
            adapter = viewAdapter
        }
    }

    override fun onResume() {
        super.onResume()
        refreshWidgetList()
    }

    private fun refreshWidgetList() {
        val appWidgetManager = AppWidgetManager.getInstance(this)
        widgetConfList.clear()
        val toggleWidgetIds = appWidgetManager.getAppWidgetIds(ComponentName(this, ToggleWidget::class.java))
        for (id in toggleWidgetIds) {
            val conf = ToggleWidgetConfigureActivity.loadConf(this, id)
            widgetConfList.add(WidgetConf(id, conf.label, conf.event, WidgetType.TOGGLE))
        }
        val numericWidgetIds = appWidgetManager.getAppWidgetIds(ComponentName(this, NumericWidget::class.java))
        for (id in numericWidgetIds) {
            val conf = NumericWidgetConfigureActivity.loadConf(this, id)
            widgetConfList.add(WidgetConf(id, conf.label, conf.event, WidgetType.NUMERIC))
        }
        viewAdapter.notifyDataSetChanged()
    }

    private enum class WidgetType {
        TOGGLE,
        NUMERIC
    }

    private data class WidgetConf(val id: Int, val label: String, val event: String, val type: WidgetType)

    private class WidgetListAdapter(private val activity: Activity, private val widgetConfList: ArrayList<WidgetConf>) : RecyclerView.Adapter<WidgetListAdapter.WidgetListViewHolder>() {
        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): WidgetListViewHolder {
            val view = LayoutInflater.from(parent.context).inflate(R.layout.widget_list_item, parent, false)
            return WidgetListViewHolder(view)
        }
        override fun onBindViewHolder(holder: WidgetListViewHolder, position: Int) {
            val widget = widgetConfList[position]
            holder.view.findViewById<TextView>(R.id.widget_label).text = widget.label
            holder.view.findViewById<TextView>(R.id.widget_event).text = widget.event
            holder.view.findViewById<TextView>(R.id.widget_type).text = widget.type.name
            holder.view.setOnClickListener {
                val intent = Intent(it.context, when(widget.type) {
                    WidgetType.TOGGLE -> ToggleWidgetConfigureActivity::class.java
                    WidgetType.NUMERIC -> NumericWidgetConfigureActivity::class.java
                })
                intent.putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, widgetConfList[position].id)
                activity.startActivityForResult(intent, CONFIGURE_REQUEST_CODE)
            }
        }
        override fun getItemCount(): Int = widgetConfList.size
        class WidgetListViewHolder(val view: View) : RecyclerView.ViewHolder(view)
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
    }

    companion object {
        private const val CONFIGURE_REQUEST_CODE = 1
    }
}
