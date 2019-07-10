package com.microsoft.bot.builder.solutions.virtualassistant.activities.settings;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.RemoteException;
import android.support.annotation.Nullable;
import android.support.design.widget.TextInputEditText;
import android.util.Log;
import android.view.KeyEvent;
import android.view.inputmethod.EditorInfo;
import android.widget.ArrayAdapter;
import android.widget.Spinner;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;

import java.util.TimeZone;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;
import butterknife.OnEditorAction;

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
    @BindView(R.id.geolocation_lat) TextInputEditText locationLat;
    @BindView(R.id.geolocation_lon) TextInputEditText locationLon;
    @BindView(R.id.history_linecount) TextInputEditText historyLinecount;
    @BindView(R.id.spinner_timezone) Spinner spinnerTimezone;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_settings;

    // STATE
    private Configuration configuration;
    private ArrayAdapter tzAdapter;
    private Gson gson;

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

    @OnEditorAction({R.id.history_linecount, R.id.service_key, R.id.service_region, R.id.bot_id, R.id.user_id, R.id.locale, R.id.geolocation_lat, R.id.geolocation_lon})
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

    private void showConfiguration(){
        try {
            final String json = speechServiceBinder.getConfiguration();
            configuration = gson.fromJson(json, new TypeToken<Configuration>(){}.getType());
            serviceKey.setText(configuration.serviceKey);
            serviceRegion.setText(configuration.serviceRegion);
            botId.setText(configuration.botId);
            userId.setText(configuration.userId);
            locale.setText(configuration.locale);
            locationLat.setText(configuration.geolat);
            locationLon.setText(configuration.geolon);

            int iHistoryLinecount = configuration.historyLinecount==null?1:configuration.historyLinecount;
            String historyLineCount = String.valueOf(iHistoryLinecount);
            historyLinecount.setText(historyLineCount);

            selectTimezone(configuration.currentTimezone);
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
            configuration.geolat = locationLat.getText().toString();
            configuration.geolon = locationLon.getText().toString();

            // history linecount
            configuration.historyLinecount = Integer.valueOf(historyLinecount.getText().toString());
            if (configuration.historyLinecount == 0) configuration.historyLinecount = 1;//do not allow 0

            // timezone
            configuration.currentTimezone = (String)tzAdapter.getItem(spinnerTimezone.getSelectedItemPosition());

            String json = gson.toJson(configuration);
            speechServiceBinder.setConfiguration(json);
        } catch (RemoteException exception){
            Log.e(LOGTAG, exception.getMessage());
        }
    }

}
