package com.microsoft.bot.builder.solutions.virtualassistant.activities.botconfiguration;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.support.design.widget.TextInputEditText;
import android.view.KeyEvent;
import android.view.inputmethod.EditorInfo;

import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;
import butterknife.OnEditorAction;

/**
 * Bot Configuration Activity - settings to change the connection to the Bot
 * Note: settings are saved when OK is pressed
 */
public class BotConfigurationActivity extends BaseActivity {

    // VIEWS
    @BindView(R.id.service_key) TextInputEditText serviceKey;
    @BindView(R.id.service_region) TextInputEditText serviceRegion;
    @BindView(R.id.bot_id) TextInputEditText botId;
    @BindView(R.id.voice_name) TextInputEditText voiceName;
    @BindView(R.id.user_id) TextInputEditText userId;
    @BindView(R.id.locale) TextInputEditText locale;
    @BindView(R.id.geolocation_lat) TextInputEditText locationLat;
    @BindView(R.id.geolocation_lon) TextInputEditText locationLon;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_bot_configuration;

    // STATE
    private Configuration configuration;

    public static Intent getNewIntent(Context context) {
        return new Intent(context, BotConfigurationActivity.class);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);
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

    @OnEditorAction({R.id.service_key, R.id.service_region, R.id.bot_id, R.id.voice_name, R.id.user_id, R.id.locale, R.id.geolocation_lat, R.id.geolocation_lon})
    boolean onEditorAction(int actionId, KeyEvent key){
        boolean handled = false;
        if (actionId == EditorInfo.IME_ACTION_SEND || (key != null && key.getKeyCode() == KeyEvent.KEYCODE_ENTER)) {
            hideKeyboardFrom(getCurrentFocus());
            handled = true;
        }
        return handled;
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

    private void showConfiguration(){
        configuration = speechServiceBinder.getConfiguration();
        serviceKey.setText(configuration.serviceKey);
        serviceRegion.setText(configuration.serviceRegion);
        botId.setText(configuration.botId);
        voiceName.setText(configuration.voiceName);
        userId.setText(configuration.userId);
        locale.setText(configuration.locale);
        locationLat.setText(configuration.geolat);
        locationLon.setText(configuration.geolon);
    }

    private void saveConfiguration(){
        configuration.serviceKey = serviceKey.getText().toString();
        configuration.serviceRegion = serviceRegion.getText().toString();
        configuration.botId = botId.getText().toString();
        configuration.voiceName = voiceName.getText().toString();
        configuration.userId = userId.getText().toString();
        configuration.locale = locale.getText().toString();
        configuration.geolat = locationLat.getText().toString();
        configuration.geolon = locationLon.getText().toString();
        speechServiceBinder.setConfiguration(configuration);
    }

}
