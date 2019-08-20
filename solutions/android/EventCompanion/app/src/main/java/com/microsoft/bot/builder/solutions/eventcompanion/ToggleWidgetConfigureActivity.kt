package com.microsoft.bot.builder.solutions.eventcompanion

import android.app.Activity
import android.appwidget.AppWidgetManager
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.view.View
import android.view.inputmethod.EditorInfo
import android.widget.*
import android.widget.AdapterView.OnItemSelectedListener
import com.google.gson.Gson
import com.squareup.picasso.Picasso

/**
 * The configuration screen for the [ToggleWidget] AppWidget.
 */
class ToggleWidgetConfigureActivity : Activity() {
    private var mAppWidgetId = AppWidgetManager.INVALID_APPWIDGET_ID
    private lateinit var widgetTemplateList: Spinner
    private lateinit var widgetLabelEdit: EditText
    private lateinit var widgetEventEdit: EditText
    private lateinit var widgetLabelPreview: TextView
    private lateinit var widgetIconPreview: ImageView
    private lateinit var widgetAddButton: Button
    private lateinit var widgetIconButton: Button
    private lateinit var widgetConf: ToggleWidgetConf
    private lateinit var widgetTemplates: ArrayList<String>

    public override fun onCreate(icicle: Bundle?) {
        super.onCreate(icicle)

        // Set the result to CANCELED.  This will cause the widget host to cancel
        // out of the widget placement if the user presses the back button.
        setResult(RESULT_CANCELED)

        setContentView(R.layout.toggle_widget_configure)

        widgetTemplateList = findViewById(R.id.widget_template_list)
        widgetLabelEdit = findViewById(R.id.widget_label_edit)
        widgetEventEdit = findViewById(R.id.widget_event_edit)
        widgetLabelPreview = findViewById(R.id.preview_text)
        widgetIconPreview = findViewById(R.id.preview_icon)
        widgetAddButton = findViewById(R.id.widget_add_button)
        widgetIconButton = findViewById(R.id.widget_icon_button)

        widgetTemplates = ArrayList()
        widgetTemplates.add("Select a template")
        widgetTemplates.addAll(assets.list("templates/toggle")!!)
        val templateListAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, widgetTemplates)
        templateListAdapter.setDropDownViewResource(android.R.layout.simple_dropdown_item_1line)
        widgetTemplateList.adapter = templateListAdapter
        widgetTemplateList.onItemSelectedListener = mOnItemSelectedListener

        widgetLabelEdit.setOnEditorActionListener { _, actionId, _ ->
            if (actionId == EditorInfo.IME_ACTION_DONE) {
                widgetLabelPreview.text = widgetLabelEdit.text
                true
            } else {
                false
            }
        }
        widgetAddButton.setOnClickListener(mOnAddWidgetListener)
        widgetIconButton.setOnClickListener(mOnSetIconListener)

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
        widgetLabelEdit.setText(widgetConf.label)
        widgetEventEdit.setText(widgetConf.event)
        if (widgetConf.icon.isNotEmpty()) {
            Picasso.get().load(widgetConf.icon).into(widgetIconPreview)
        }
        widgetLabelPreview.text = widgetConf.label
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)
        if (requestCode == READ_REQUEST_CODE && resultCode == RESULT_OK) {
            data?.data?.also { uri ->
                widgetConf.icon = uri.toString()
                Picasso.get().load(widgetConf.icon).into(widgetIconPreview)
            }
        }
    }

    private var mOnAddWidgetListener: View.OnClickListener = View.OnClickListener {
        val context = this@ToggleWidgetConfigureActivity

        // When the button is clicked, store the widget label, event & icon uri locally
        widgetConf.label = widgetLabelEdit.text.toString()
        widgetConf.event = widgetEventEdit.text.toString()
        saveConf(context, mAppWidgetId, widgetConf)

        // It is the responsibility of the configuration activity to update the app widget
        val appWidgetManager = AppWidgetManager.getInstance(context)
        ToggleWidget.updateAppWidget(context, appWidgetManager, mAppWidgetId)

        // Make sure we pass back the original appWidgetId
        val resultValue = Intent()
        resultValue.putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, mAppWidgetId)
        setResult(RESULT_OK, resultValue)
        finish()
    }

    private var mOnSetIconListener: View.OnClickListener = View.OnClickListener {
        val intent = Intent(Intent.ACTION_OPEN_DOCUMENT).apply {
            addCategory(Intent.CATEGORY_OPENABLE)
            type = "image/*"
        }
        startActivityForResult(intent, READ_REQUEST_CODE)
    }

    private var mOnItemSelectedListener = object : OnItemSelectedListener {
        override fun onNothingSelected(parent: AdapterView<*>?) {
        }
        override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
            if (position == 0) {
                return
            }
            val fileName = widgetTemplates[position]
            val jsonString = applicationContext.assets.open("templates/toggle/$fileName").bufferedReader().use { it.readText() }
            widgetConf = Gson().fromJson(jsonString, ToggleWidgetConf::class.java)
            widgetLabelEdit.setText(widgetConf.label)
            widgetEventEdit.setText(widgetConf.event)
            if (widgetConf.icon.isNotEmpty()) {
                Picasso.get().load(widgetConf.icon).into(widgetIconPreview)
            }
            widgetLabelPreview.text = widgetConf.label
        }
    }

    companion object {

        private const val READ_REQUEST_CODE: Int = 42
        private const val PREFS_NAME = "com.microsoft.bot.builder.solutions.eventcompanion.ToggleWidget"
        private const val PREF_DATA_PREFIX = "data_"
        private const val PREF_CONF_PREFIX = "conf_"

        internal data class ToggleWidgetData(var value: Boolean = false)
        internal data class ToggleWidgetConf(var label: String = "", var event: String = "", var icon: String = "")

        internal fun saveData(context: Context, appWidgetId: Int, data: ToggleWidgetData) {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            val dataString = Gson().toJson(data)
            prefs.putString(PREF_DATA_PREFIX + appWidgetId, dataString)
            prefs.apply()
        }

        internal fun loadData(context: Context, appWidgetId: Int): ToggleWidgetData {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0)
            val dataString = prefs.getString(PREF_DATA_PREFIX + appWidgetId, "{}")
            return Gson().fromJson(dataString, ToggleWidgetData::class.java)
        }

        internal fun removeData(context: Context, appWidgetId: Int) {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            prefs.remove(PREF_DATA_PREFIX + appWidgetId)
            prefs.apply()
        }

        internal fun saveConf(context: Context, appWidgetId: Int, conf: ToggleWidgetConf) {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            val confString = Gson().toJson(conf)
            prefs.putString(PREF_CONF_PREFIX + appWidgetId, confString)
            prefs.apply()
        }

        internal fun loadConf(context: Context, appWidgetId: Int): ToggleWidgetConf {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0)
            val confString = prefs.getString(PREF_CONF_PREFIX + appWidgetId, "{}")
            return Gson().fromJson(confString, ToggleWidgetConf::class.java)
        }

        internal fun removeConf(context: Context, appWidgetId: Int) {
            val prefs = context.getSharedPreferences(PREFS_NAME, 0).edit()
            prefs.remove(PREF_CONF_PREFIX + appWidgetId)
            prefs.apply()
        }
    }
}

