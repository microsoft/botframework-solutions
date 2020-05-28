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
import android.support.v7.widget.SwitchCompat;
import android.util.Log;
import android.view.KeyEvent;
import android.view.View;
import android.view.inputmethod.EditorInfo;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.EditText;
import android.widget.Spinner;

import com.google.gson.Gson;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;
import com.microsoft.bot.builder.solutions.virtualassistant.utils.AppConfiguration;
import com.skydoves.colorpickerview.ColorPickerDialog;
import com.skydoves.colorpickerview.listeners.ColorEnvelopeListener;

import org.greenrobot.eventbus.EventBus;
import org.greenrobot.eventbus.Subscribe;
import org.greenrobot.eventbus.ThreadMode;

import java.io.IOException;
import java.util.TimeZone;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnCheckedChanged;
import butterknife.OnClick;
import butterknife.OnEditorAction;
import butterknife.OnTextChanged;
import events.GpsLocationSent;

/**
 * Bot Configuration Activity - settings to change the connection to the Bot
 * Note: settings are saved when OK is pressed
 */
public class SettingsActivity extends BaseActivity {

    // VIEWS
    @BindView(R.id.speech_subscription_key) TextInputEditText serviceKey;
    @BindView(R.id.speech_region) TextInputEditText serviceRegion;
    @BindView(R.id.custom_commands_app_id) TextInputEditText customCommandsAppId;
    @BindView(R.id.custom_voice_deployment_ids) TextInputEditText customVoiceDeploymentIds;
    @BindView(R.id.custom_sr_endpoint_id) TextInputEditText customSpeechRecognitionEndpointId;
    @BindView(R.id.user_id) TextInputEditText userId;
    @BindView(R.id.sr_language) TextInputEditText srLanguage;
    @BindView(R.id.history_linecount) TextInputEditText historyLinecount;
    @BindView(R.id.switch_enable_sdk_logging) SwitchCompat switchEnableSdkLogging;
    @BindView(R.id.spinner_timezone) Spinner spinnerTimezone;
    @BindView(R.id.color_picker_bot) View colorPickedBot;
    @BindView(R.id.color_picker_user) View colorPickedUser;
    @BindView(R.id.color_picker_bot_text) View colorPickedBotText;
    @BindView(R.id.color_picker_user_text) View colorPickedUserText;
    @BindView(R.id.edit_color_picked_bot) EditText colorPickedBotEditText;
    @BindView(R.id.edit_color_picked_user) EditText colorPickedUserEditText;
    @BindView(R.id.edit_color_picked_bot_text) EditText colorPickedBotTextEditText;
    @BindView(R.id.edit_color_picked_user_text) EditText colorPickedUserTextEditText;
    @BindView(R.id.edit_time_text) EditText gpsSentTimeEditText;
    @BindView(R.id.switch_show_full_conversation) SwitchCompat switchShowFullConversation;
    @BindView(R.id.switch_enable_dark_mode) SwitchCompat switchEnableDarkMode;
    @BindView(R.id.switch_keep_screen_on) SwitchCompat switchKeepScreenOn;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_settings;

    // STATE
    private Configuration configuration;
    private AppConfiguration appConfiguration;
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
        EventBus.getDefault().register(this);
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
        super.onStop();
        if (myConnection != null) {
            unbindService(myConnection);
            speechServiceBinder = null;
        }
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        EventBus.getDefault().unregister(this);
    }

    @Override
    protected void serviceConnected() {
        showConfiguration();
        showAppConfiguration();
    }

    @OnEditorAction({R.id.history_linecount, R.id.speech_subscription_key, R.id.speech_region, R.id.user_id, R.id.sr_language, R.id.custom_commands_app_id, R.id.custom_voice_deployment_ids, R.id.custom_sr_endpoint_id})
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
            colorBubbleBot = updatedColor;
        } catch (NumberFormatException ex){
            //nothing to do if the number is not legal
        }
    }

    @OnTextChanged(value = R.id.edit_color_picked_user, callback = OnTextChanged.Callback.AFTER_TEXT_CHANGED)
    public void changedColorTextBgUser(CharSequence text) {
        try {
            int updatedColor = Integer.parseInt(text.toString(), 16) | 0xFF000000;
            updateShapeColor(colorPickedUser, updatedColor);
            colorBubbleUser = updatedColor;
        } catch (NumberFormatException ex){
            //nothing to do if the number is not legal
        }
    }

    @OnTextChanged(value = R.id.edit_color_picked_bot_text, callback = OnTextChanged.Callback.AFTER_TEXT_CHANGED)
    public void changedColorTextBot(CharSequence text) {
        try {
            int updatedColor = Integer.parseInt(text.toString(), 16) | 0xFF000000;
            updateShapeColor(colorPickedBotText, updatedColor);
            colorTextBot = updatedColor;
        } catch (NumberFormatException ex){
            //nothing to do if the number is not legal
        }
    }

    @OnTextChanged(value = R.id.edit_color_picked_user_text, callback = OnTextChanged.Callback.AFTER_TEXT_CHANGED)
    public void changedColorTextUser(CharSequence text) {
        try {
            int updatedColor = Integer.parseInt(text.toString(), 16) | 0xFF000000;
            updateShapeColor(colorPickedUserText, updatedColor);
            colorTextUser = updatedColor;
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

    @OnCheckedChanged(R.id.switch_enable_sdk_logging)
    public void onChangeEnableSDKLogging(boolean checked) {
        configuration.speechSdkLogEnabled = checked;
    }

    @OnCheckedChanged(R.id.switch_show_full_conversation)
    public void onChangeShowFullConversation(boolean checked) {
        appConfiguration.showFullConversation = checked;
    }

    @OnCheckedChanged(R.id.switch_enable_dark_mode)
    public void onChangeEnableDarkMode(boolean checked) {
        appConfiguration.enableDarkMode = checked;
    }

    @OnCheckedChanged(R.id.switch_keep_screen_on)
    public void onChangeKeepScreenOn(boolean checked) {
        appConfiguration.keepScreenOn = checked;
    }

    @OnClick(R.id.btn_send_gps)
    public void onClickSendGps() {
        try {
            speechServiceBinder.sendLocationUpdate();
        } catch (RemoteException e) {
            e.printStackTrace();
        }
    }

    @OnClick(R.id.btn_cancel)
    public void onClickCancel() {
        setResult(RESULT_CANCELED);
        finish();
    }

    @OnClick(R.id.btn_save)
    public void onClickSave() {
        saveConfiguration();// must save updated config first
        saveAppConfiguration();
        initializeAndConnect();// re-init service to make it read updated config
        setResult(RESULT_OK);
        finish();
    }

    // EventBus: the GPS location was sent
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEventGpsLocationSent(GpsLocationSent event) {
        showGpsLocationSentDate();
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
        configuration = configurationManager.getConfiguration();

        serviceKey.setText(configuration.speechSubscriptionKey);
        serviceRegion.setText(configuration.speechRegion);
        customCommandsAppId.setText(configuration.customCommandsAppId);
        customVoiceDeploymentIds.setText(configuration.customVoiceDeploymentIds);
        customSpeechRecognitionEndpointId.setText(configuration.customSREndpointId);
        userId.setText(configuration.userId);
        srLanguage.setText(configuration.srLanguage);
        switchEnableSdkLogging.setChecked(configuration.speechSdkLogEnabled);

        // timezone
        selectTimezone(configuration.currentTimezone);

        // keywords
        Spinner spinner = findViewById(R.id.keyword_dropdown);
        ArrayAdapter adapter = new ArrayAdapter<String>(this,android.R.layout.simple_spinner_dropdown_item,keywords);
        int currentKeywordPosition = adapter.getPosition(configuration.keyword);
        spinner.setAdapter(adapter);
        spinner.setSelection(currentKeywordPosition);
        spinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                configuration.keyword = keywords[position];
            }
            @Override
            public void onNothingSelected(AdapterView<?> parent) {
            }
        });

        // gps sent time
        showGpsLocationSentDate();
    }

    private void showAppConfiguration() {
        appConfiguration = appConfigurationManager.getConfiguration();

        // history linecount
        String historyLineCount = String.valueOf(appConfiguration.historyLinecount);
        historyLinecount.setText(historyLineCount);


        // chat bubble colors
        colorBubbleBot = appConfiguration.colorBubbleBot;
        colorBubbleUser = appConfiguration.colorBubbleUser;
        updateShapeColor(colorPickedBot, colorBubbleBot);
        updateShapeColor(colorPickedUser, colorBubbleUser);
        colorPickedBotEditText.setText(String.format("%06X", colorBubbleBot & 0xFFFFFF));
        colorPickedUserEditText.setText(String.format("%06X", colorBubbleUser & 0xFFFFFF));

        // text colors
        colorTextBot = appConfiguration.colorTextBot;
        colorTextUser = appConfiguration.colorTextUser;
        updateShapeColor(colorPickedBotText, colorTextBot);
        updateShapeColor(colorPickedUserText, colorTextUser);
        colorPickedBotTextEditText.setText(String.format("%06X", colorTextBot & 0xFFFFFF));
        colorPickedUserTextEditText.setText(String.format("%06X", colorTextUser & 0xFFFFFF));

        // show full conversation
        switchShowFullConversation.setChecked(appConfiguration.showFullConversation);

        // enable dark mode
        switchEnableDarkMode.setChecked(appConfiguration.enableDarkMode);

        // keep screen on
        switchKeepScreenOn.setChecked(appConfiguration.keepScreenOn);
    }

    private void showGpsLocationSentDate() {
        String gpsTime = null;
        try {
            gpsTime = speechServiceBinder.getDateSentLocationEvent();
        } catch (RemoteException e) {
            e.printStackTrace();
        }
        if (gpsTime == null)
            gpsSentTimeEditText.setText(R.string.configuration_location_unsent);
        else
            gpsSentTimeEditText.setText(gpsTime);
    }

    private void saveConfiguration() {
        configuration.speechSubscriptionKey = serviceKey.getText().toString();
        configuration.speechRegion = serviceRegion.getText().toString();
        configuration.customCommandsAppId = customCommandsAppId.getText().toString();
        configuration.customVoiceDeploymentIds = customVoiceDeploymentIds.getText().toString();
        configuration.customSREndpointId = customSpeechRecognitionEndpointId.getText().toString();
        configuration.userId = userId.getText().toString();
        configuration.srLanguage = srLanguage.getText().toString();

        // timezone
        configuration.currentTimezone = (String)tzAdapter.getItem(spinnerTimezone.getSelectedItemPosition());

        configurationManager.setConfiguration(configuration);
    }

    private void saveAppConfiguration() {
        // history linecount
        appConfiguration.historyLinecount = Integer.valueOf(historyLinecount.getText().toString());
        if (appConfiguration.historyLinecount == 0) appConfiguration.historyLinecount = 1;//do not allow 0

        // chat bubble colors
        appConfiguration.colorBubbleBot = colorBubbleBot;
        appConfiguration.colorBubbleUser = colorBubbleUser;

        // text colors
        appConfiguration.colorTextBot = colorTextBot;
        appConfiguration.colorTextUser = colorTextUser;

        appConfigurationManager.setConfiguration(appConfiguration);
    }

}
