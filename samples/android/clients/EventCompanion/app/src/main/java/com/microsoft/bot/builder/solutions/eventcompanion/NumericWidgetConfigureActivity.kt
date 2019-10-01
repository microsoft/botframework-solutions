package com.microsoft.bot.builder.solutions.eventcompanion

import android.app.Activity
import android.appwidget.AppWidgetManager
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import android.widget.*
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.gson.Gson

/**
 * The configuration screen for the [NumericWidget] AppWidget.
 */
class NumericWidgetConfigureActivity : Activity() {
    private var mAppWidgetId = AppWidgetManager.INVALID_APPWIDGET_ID
    private lateinit var widgetTemplateList: Spinner
    private lateinit var widgetLabelEdit: EditText
    private lateinit var widgetEventEdit: EditText
    private lateinit var widgetUnitEdit: EditText
    private lateinit var widgetMinEdit: EditText
    private lateinit var widgetMaxEdit: EditText
    private lateinit var widgetLabelPreview: TextView
    private lateinit var widgetAddRangeButton: Button
    private lateinit var widgetAddButton: Button
    private lateinit var widgetValueRanges: RecyclerView
    private lateinit var widgetValueRangesAdapter: WidgetValueRangesAdapter
    private lateinit var widgetConf: NumericWidgetConf
    private lateinit var widgetTemplates: ArrayList<String>

    public override fun onCreate(icicle: Bundle?) {
        super.onCreate(icicle)

        // Set the result to CANCELED.  This will cause the widget host to cancel
        // out of the widget placement if the user presses the back button.
        setResult(RESULT_CANCELED)

        setContentView(R.layout.numeric_widget_configure)
        widgetTemplateList = findViewById(R.id.widget_template_list)
        widgetLabelEdit = findViewById(R.id.widget_label_edit)
        widgetEventEdit = findViewById(R.id.widget_event_edit)
        widgetUnitEdit = findViewById(R.id.widget_unit_edit)
        widgetMinEdit = findViewById(R.id.widget_min_edit)
        widgetMaxEdit = findViewById(R.id.widget_max_edit)
        widgetLabelPreview = findViewById(R.id.preview_label)

        widgetAddRangeButton = findViewById(R.id.widget_add_range_button)
        widgetAddButton = findViewById(R.id.widget_add_button)

        widgetValueRanges = findViewById(R.id.widget_value_ranges)

        widgetTemplates = ArrayList()
        widgetTemplates.add("Select a template")
        widgetTemplates.addAll(assets.list("templates/numeric")!!)
        val templateListAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, widgetTemplates)
        templateListAdapter.setDropDownViewResource(android.R.layout.simple_dropdown_item_1line)
        widgetTemplateList.adapter = templateListAdapter
        widgetTemplateList.onItemSelectedListener = mOnItemSelectedListener

        widgetLabelEdit.setOnEditorActionListener { _, actionId, _ ->
            if (actionId == EditorInfo.IME_ACTION_DONE) {
                val label = widgetLabelEdit.text
                widgetLabelPreview.text = label
                true
            } else {
                false
            }
        }

        widgetAddRangeButton.setOnClickListener {
            val unit = widgetUnitEdit.text.toString()
            val minimum = widgetMinEdit.text.toString()
            val maximum = widgetMaxEdit.text.toString()
            if (minimum.isNotEmpty() && maximum.isNotEmpty()) {
                widgetConf.ranges.add(NumericWidgetValueRange(unit, minimum.toFloat(), maximum.toFloat()))
                widgetValueRangesAdapter.notifyItemInserted(widgetConf.ranges.size)
            }
        }
        widgetAddButton.setOnClickListener(mOnAddWidgetListener)

        // Find the widget id from the intent.
        val intent = intent
        val extras = intent.extras
        if (extras != null) {
            mAppWidgetId = extras.getInt(
                AppWidgetManager.EXTRA_APPWIDGET_ID, AppWidgetManager.INVALID_APPWIDGET_ID
            )
        }

        // If this activity was started with an intent without an app widget ID, finish with an error.
        if (mAppWidgetId == AppWidgetManager.INVALID_APPWIDGET_ID) {
            finish()
            return
        }

        val callingActivity = this.callingActivity
        if (callingActivity != null) {
            if (callingActivity.className == MainActivity::class.java.name) {
                widgetAddButton.setText(R.string.save_widget)
            }
        }

        widgetConf = loadConf(this, mAppWidgetId)
        widgetValueRangesAdapter = WidgetValueRangesAdapter(widgetConf.ranges)
        widgetValueRanges.apply {
            layoutManager = LinearLayoutManager(this@NumericWidgetConfigureActivity)
            adapter = widgetValueRangesAdapter
        }

        widgetLabelEdit.setText(widgetConf.label)
        widgetEventEdit.setText(widgetConf.event)
        widgetLabelPreview.text = widgetConf.label
    }

    private var mOnAddWidgetListener: View.OnClickListener = View.OnClickListener {
        val context = this@NumericWidgetConfigureActivity

        // When the button is clicked, store the string locally
        widgetConf.label = widgetLabelEdit.text.toString()
        widgetConf.event = widgetEventEdit.text.toString()
        saveConf(context, mAppWidgetId, widgetConf)

        // It is the responsibility of the configuration activity to update the app widget
        val appWidgetManager = AppWidgetManager.getInstance(context)
        NumericWidget.updateAppWidget(context, appWidgetManager, mAppWidgetId)

        // Make sure we pass back the original appWidgetId
        val resultValue = Intent()
        resultValue.putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, mAppWidgetId)
        setResult(RESULT_OK, resultValue)
        finish()
    }

    private var mOnItemSelectedListener = object : AdapterView.OnItemSelectedListener {
        override fun onNothingSelected(parent: AdapterView<*>?) {
        }
        override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
            if (position == 0) {
                return
            }
            val fileName = widgetTemplates[position]
            val jsonString = applicationContext.assets.open("templates/numeric/$fileName").bufferedReader().use { it.readText() }
            widgetConf = Gson().fromJson(jsonString, NumericWidgetConf::class.java)
            widgetLabelEdit.setText(widgetConf.label)
            widgetEventEdit.setText(widgetConf.event)
            widgetValueRangesAdapter.setWidgetValueRanges(widgetConf.ranges)
            widgetLabelPreview.text = widgetConf.label
        }
    }

    private class WidgetValueRangesAdapter(private var widgetValueRanges: ArrayList<NumericWidgetValueRange>): RecyclerView.Adapter<WidgetValueRangesAdapter.ViewHolder>() {
        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
            val view = LayoutInflater.from(parent.context).inflate(R.layout.numeric_widget_value_range_item, parent, false)
            val viewHolder = ViewHolder(view)
            viewHolder.itemView.findViewById<Button>(R.id.range_remove).setOnClickListener {
                val position = viewHolder.adapterPosition
                val widgetValueRange = widgetValueRanges[position]
                widgetValueRanges.remove(widgetValueRange)
                this@WidgetValueRangesAdapter.notifyItemRemoved(position)
            }
            return viewHolder
        }
        override fun getItemCount(): Int {
            return widgetValueRanges.size
        }
        override fun onBindViewHolder(holder: ViewHolder, position: Int) {
            val widgetValueRange = widgetValueRanges[position]
            holder.itemView.findViewById<TextView>(R.id.range_unit).text = widgetValueRange.unit
            holder.itemView.findViewById<TextView>(R.id.range_min).text = widgetValueRange.minimum.toString()
            holder.itemView.findViewById<TextView>(R.id.range_max).text = widgetValueRange.maximum.toString()
        }
        fun setWidgetValueRanges(widgetValueRanges: ArrayList<NumericWidgetValueRange>) {
            this.widgetValueRanges = widgetValueRanges
            this.notifyDataSetChanged()
        }
        class ViewHolder(view: View) : RecyclerView.ViewHolder(view)
    }

    companion object {
        private const val PREFS_NAME = "com.microsoft.bot.builder.solutions.eventcompanion.NumericWidget"
        private const val PREF_DATA_PREFIX = "data_"
        private const val PREF_CONF_PREFIX = "conf_"

        internal data class NumericWidgetData(var value: Float = 20F, var unit: String = "")
        internal data class NumericWidgetValueRange(var unit: String = "", var minimum: Float = 0F, var maximum: Float = 100F)
        internal data class NumericWidgetConf(var label: String = "", var event: String = "", var ranges: ArrayList<NumericWidgetValueRange> = ArrayList())

        internal fun saveData(context: Context, appWidgetId: Int, data: NumericWidgetData) {
            val dataJson = Gson().toJson(data)
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            prefs.putString(PREF_DATA_PREFIX + appWidgetId, dataJson)
            prefs.apply()
        }

        internal fun loadData(context: Context, appWidgetId: Int): NumericWidgetData {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0)
            val dataString = prefs.getString(PREF_DATA_PREFIX + appWidgetId, "{}")
            return Gson().fromJson(dataString, NumericWidgetData::class.java)
        }

        internal fun removeData(context: Context, appWidgetId: Int) {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            prefs.remove(PREF_DATA_PREFIX + appWidgetId)
            prefs.apply()
        }

        internal fun saveConf(context: Context, appWidgetId: Int, conf: NumericWidgetConf) {
            val confJson = Gson().toJson(conf)
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            prefs.putString(PREF_CONF_PREFIX + appWidgetId, confJson)
            prefs.apply()
        }

        internal fun loadConf(context: Context, appWidgetId: Int): NumericWidgetConf {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0)
            val confString = prefs.getString(PREF_CONF_PREFIX + appWidgetId, "{}")
            return Gson().fromJson(confString, NumericWidgetConf::class.java)
        }

        internal fun removeConf(context: Context, appWidgetId: Int) {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            prefs.remove(PREF_CONF_PREFIX + appWidgetId)
            prefs.apply()
        }
    }
}

