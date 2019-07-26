package com.microsoft.bot.builder.solutions.virtualassistant.activities.settings;

import android.app.AlertDialog;
import android.content.Context;
import android.content.Intent;
import android.content.res.AssetManager;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.os.RemoteException;
import android.support.annotation.Nullable;
import android.support.design.widget.TextInputEditText;
import android.util.Log;
import android.view.KeyEvent;
import android.view.View;
import android.view.inputmethod.EditorInfo;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.EditText;
import android.widget.Spinner;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;
import com.skydoves.colorpickerview.ColorPickerDialog;
import com.skydoves.colorpickerview.listeners.ColorEnvelopeListener;

import java.io.IOException;
import java.util.TimeZone;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;
import butterknife.OnEditorAction;
import butterknife.OnTextChanged;

/**
 * Bot Configuration Activity - settings to change the connection to the Bot
 * Note: settings are saved when OK is pressed
 */
public class SettingsActivity extends BaseActivity {

    // VIEWS
    @BindView(R.id.service_key) TextInputEditText serviceKey;
    @BindView(R.id.service_region) TextInputEditText serviceRegion;
    @BindView(R.id.bot_id) TextInputEditText botId;
    @BindView(R.id.user_id) TextInputEditText userId;
    @BindView(R.id.locale) TextInputEditText locale;
    @BindView(R.id.history_linecount) TextInputEditText historyLinecount;
    @BindView(R.id.spinner_timezone) Spinner spinnerTimezone;
    @BindView(R.id.color_picker_bot) View colorPickedBot;
    @BindView(R.id.color_picker_user) View colorPickedUser;
    @BindView(R.id.color_picker_bot_text) View colorPickedBotText;
    @BindView(R.id.color_picker_user_text) View colorPickedUserText;
    @BindView(R.id.edit_color_picked_bot) EditText colorPickedBotEditText;
    @BindView(R.id.edit_color_picked_user) EditText colorPickedUserEditText;
    @BindView(R.id.edit_color_picked_bot_text) EditText colorPickedBotTextEditText;
    @BindView(R.id.edit_color_picked_user_text) EditText colorPickedUserTextEditText;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_settings;

    // STATE
    private Configuration configuration;
    private ArrayAdapter tzAdapter;
    private Gson gson;
    private Integer colorBubbleBot, colorBubbleUser, colorTextBot, colorTextUser;
    private String[] keywords;

    public static Intent getNewIntent(Context context) {
        return new Intent(context, SettingsActivity.class);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);
        gson = new Gson();
        initTimezoneAdapter();

        AssetManager am = getAssets();
        try {
            keywords = am.list("keywords");
        }
        catch (IOException e){
            Log.e(LOGTAG, e.getMessage());
        }
    }

    @Override
    public void onStart() {
        super.onStart();
        if (speechServiceBinder == null) {
            doBindService();
        }
    }

    @Override
    public void onStop() {
        if (speechServiceBinder != null) {
            unbindService(myConnection);
            speechServiceBinder = null;
        }
        super.onStop();
    }

    @Override
    protected void serviceConnected() {
        showConfiguration();
    }

    @OnEditorAction({R.id.history_linecount, R.id.service_key, R.id.service_region, R.id.bot_id, R.id.user_id, R.id.locale})
    boolean onEditorAction(int actionId, KeyEvent key){
        boolean handled = false;
        if (actionId == EditorInfo.IME_ACTION_SEND || (key != null && key.getKeyCode() == KeyEvent.KEYCODE_ENTER)) {
            hideKeyboardFrom(getCurrentFocus());
            handled = true;
        }
        return handled;
    }

    @OnTextChanged(value = R.id.edit_color_picked_bot, callback = OnTextChanged.Callback.AFTER_TEXT_CHANGED)
    public void changedColorTextBgBot(CharSequence text) {
        try {
            int updatedColor = Integer.parseInt(text.toString(), 16) | 0xFF000000;
            updateShapeColor(colorPickedBot, updatedColor);
        } catch (NumberFormatException ex){
            //nothing to do if the number is not legal
        }
    }

    @OnTextChanged(value = R.id.edit_color_picked_user, callback = OnTextChanged.Callback.AFTER_TEXT_CHANGED)
    public void changedColorTextBgUser(CharSequence text) {
        try {
            int updatedColor = Integer.parseInt(text.toString(), 16) | 0xFF000000;
            updateShapeColor(colorPickedUser, updatedColor);
        } catch (NumberFormatException ex){
            //nothing to do if the number is not legal
        }
    }

    @OnTextChanged(value = R.id.edit_color_picked_bot_text, callback = OnTextChanged.Callback.AFTER_TEXT_CHANGED)
    public void changedColorTextBot(CharSequence text) {
        try {
            int updatedColor = Integer.parseInt(text.toString(), 16) | 0xFF000000;
            updateShapeColor(colorPickedBotText, updatedColor);
        } catch (NumberFormatException ex){
            //nothing to do if the number is not legal
        }
    }

    @OnTextChanged(value = R.id.edit_color_picked_user_text, callback = OnTextChanged.Callback.AFTER_TEXT_CHANGED)
    public void changedColorTextUser(CharSequence text) {
        try {
            int updatedColor = Integer.parseInt(text.toString(), 16) | 0xFF000000;
            updateShapeColor(colorPickedUserText, updatedColor);
        } catch (NumberFormatException ex){
            //nothing to do if the number is not legal
        }
    }

    @OnClick(R.id.color_picker_bot)
    public void onClickPickColorBot() {
        ColorPickerDialog.Builder builder = new ColorPickerDialog.Builder(this, AlertDialog.THEME_DEVICE_DEFAULT_DARK);
        builder.setTitle(R.string.configuration_pick_color);
        builder.setFlagView(new ColorFlag(this, R.layout.item_color_flag));
        builder.setPositiveButton(getString(R.string.ok), (ColorEnvelopeListener) (envelope, fromUser) -> {
            colorBubbleBot = envelope.getColor();
            updateShapeColor(colorPickedBot, colorBubbleBot);
            colorPickedBotEditText.setText(String.format("%06X", colorBubbleBot & 0xFFFFFF));
        });
        builder.setNegativeButton(getString(R.string.cancel), (dialogInterface, i) -> dialogInterface.dismiss());
        builder.attachBrightnessSlideBar();
        builder.show();
    }

    @OnClick(R.id.color_picker_user)
    public void onClickPickColorUser() {
        ColorPickerDialog.Builder builder = new ColorPickerDialog.Builder(this, AlertDialog.THEME_DEVICE_DEFAULT_DARK);
        builder.setTitle(R.string.configuration_pick_color);
        builder.setFlagView(new ColorFlag(this, R.layout.item_color_flag));
        builder.setPositiveButton(getString(R.string.ok), (ColorEnvelopeListener) (envelope, fromUser) -> {
            colorBubbleUser = envelope.getColor();
            updateShapeColor(colorPickedUser, colorBubbleUser);
            colorPickedUserEditText.setText(String.format("%06X", colorBubbleUser & 0xFFFFFF));
        });
        builder.setNegativeButton(getString(R.string.cancel), (dialogInterface, i) -> dialogInterface.dismiss());
        builder.attachBrightnessSlideBar();
        builder.show();
    }

    @OnClick(R.id.color_picker_bot_text)
    public void onClickPickColorBotText() {
        ColorPickerDialog.Builder builder = new ColorPickerDialog.Builder(this, AlertDialog.THEME_DEVICE_DEFAULT_DARK);
        builder.setTitle(R.string.configuration_pick_color);
        builder.setFlagView(new ColorFlag(this, R.layout.item_color_flag));
        builder.setPositiveButton(getString(R.string.ok), (ColorEnvelopeListener) (envelope, fromUser) -> {
            colorTextBot = envelope.getColor();
            updateShapeColor(colorPickedBotText, colorTextBot);
            colorPickedBotTextEditText.setText(String.format("%06X", colorTextBot & 0xFFFFFF));
        });
        builder.setNegativeButton(getString(R.string.cancel), (dialogInterface, i) -> dialogInterface.dismiss());
        builder.attachBrightnessSlideBar();
        builder.show();
    }

    @OnClick(R.id.color_picker_user_text)
    public void onClickPickColorUserText() {
        ColorPickerDialog.Builder builder = new ColorPickerDialog.Builder(this, AlertDialog.THEME_DEVICE_DEFAULT_DARK);
        builder.setTitle(R.string.configuration_pick_color);
        builder.setFlagView(new ColorFlag(this, R.layout.item_color_flag));
        builder.setPositiveButton(getString(R.string.ok), (ColorEnvelopeListener) (envelope, fromUser) -> {
            colorTextUser = envelope.getColor();
            updateShapeColor(colorPickedUserText, colorTextUser);
            colorPickedUserTextEditText.setText(String.format("%06X", colorTextUser & 0xFFFFFF));
        });
        builder.setNegativeButton(getString(R.string.cancel), (dialogInterface, i) -> dialogInterface.dismiss());
        builder.attachBrightnessSlideBar();
        builder.show();
    }

    @OnClick(R.id.btn_cancel)
    public void onClickCancel() {
        finish();
    }

    @OnClick(R.id.btn_ok)
    public void onClickOk() {
        saveConfiguration();// must save updated config first
        initializeAndConnect();// re-init service to make it read updated config
        finish();
    }

    private void initTimezoneAdapter() {

        //populate spinner with all timezones
        String[] idArray = TimeZone.getAvailableIDs();
        tzAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, idArray);
        tzAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spinnerTimezone.setAdapter(tzAdapter);
    }

    private void selectTimezone(@Nullable String tzId) {
        if (tzId == null){
            tzId = TimeZone.getDefault().getID();
        }
        for (int i = 0; i < tzAdapter.getCount(); i++) {
            if (tzAdapter.getItem(i).equals(tzId)) {
                spinnerTimezone.setSelection(i);
                break;
            }
        }
    }

    private void updateShapeColor(View view, int color){
        GradientDrawable gradientDrawable = (GradientDrawable) view.getBackground();
        gradientDrawable.setColor(color);
    }

    private void showConfiguration(){
        try {
            final String json = speechServiceBinder.getConfiguration();
            configuration = gson.fromJson(json, new TypeToken<Configuration>(){}.getType());
            serviceKey.setText(configuration.serviceKey);
            serviceRegion.setText(configuration.serviceRegion);
            botId.setText(configuration.botId);
            userId.setText(configuration.userId);
            locale.setText(configuration.locale);

            // history linecount
            int iHistoryLinecount = configuration.historyLinecount==null?1:configuration.historyLinecount;
            String historyLineCount = String.valueOf(iHistoryLinecount);
            historyLinecount.setText(historyLineCount);

            // timezone
            selectTimezone(configuration.currentTimezone);

            // chat bubble colors
            colorBubbleBot = configuration.colorBubbleBot;
            colorBubbleUser = configuration.colorBubbleUser;
            updateShapeColor(colorPickedBot, colorBubbleBot);
            updateShapeColor(colorPickedUser, colorBubbleUser);
            colorPickedBotEditText.setText(String.format("%06X", colorBubbleBot & 0xFFFFFF));
            colorPickedUserEditText.setText(String.format("%06X", colorBubbleUser & 0xFFFFFF));

            // text colors
            colorTextBot = configuration.colorTextBot;
            colorTextUser = configuration.colorTextUser;
            updateShapeColor(colorPickedBotText, colorTextBot);
            updateShapeColor(colorPickedUserText, colorTextUser);
            colorPickedBotTextEditText.setText(String.format("%06X", colorTextBot & 0xFFFFFF));
            colorPickedUserTextEditText.setText(String.format("%06X", colorTextUser & 0xFFFFFF));

            // keywords
            Spinner spinner = findViewById(R.id.keyword_dropdown);
            spinner.setAdapter(new ArrayAdapter<String>(this,android.R.layout.simple_spinner_dropdown_item,keywords));
            spinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
                @Override
                public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                    configuration.keyword = keywords[position];
                }
                @Override
                public void onNothingSelected(AdapterView<?> parent) {
                }
            });


        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }
    }

    private void saveConfiguration(){
        try {
            configuration.serviceKey = serviceKey.getText().toString();
            configuration.serviceRegion = serviceRegion.getText().toString();
            configuration.botId = botId.getText().toString();
            configuration.userId = userId.getText().toString();
            configuration.locale = locale.getText().toString();

            // history linecount
            configuration.historyLinecount = Integer.valueOf(historyLinecount.getText().toString());
            if (configuration.historyLinecount == 0) configuration.historyLinecount = 1;//do not allow 0

            // timezone
            configuration.currentTimezone = (String)tzAdapter.getItem(spinnerTimezone.getSelectedItemPosition());

            // chat bubble colors
            configuration.colorBubbleBot = colorBubbleBot;
            configuration.colorBubbleUser = colorBubbleUser;

            // text colors
            configuration.colorTextBot = colorTextBot;
            configuration.colorTextUser = colorTextUser;

            // note: keyword is stored in showConfiguration()$OnItemSelectedListener

            String json = gson.toJson(configuration);
            speechServiceBinder.setConfiguration(json);
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }
    }

}
