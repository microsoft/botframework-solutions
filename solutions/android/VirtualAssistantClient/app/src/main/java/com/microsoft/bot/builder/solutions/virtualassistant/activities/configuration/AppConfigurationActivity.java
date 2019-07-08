package com.microsoft.bot.builder.solutions.virtualassistant.activities.configuration;

import android.Manifest;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.RemoteException;
import android.support.annotation.Nullable;
import android.support.design.widget.TextInputEditText;
import android.support.v4.app.ActivityCompat;
import android.util.Log;
import android.view.KeyEvent;
import android.view.inputmethod.EditorInfo;
import android.widget.ArrayAdapter;
import android.widget.Spinner;

import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.ISpeechService;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;

import org.greenrobot.eventbus.EventBus;

import java.util.TimeZone;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;
import butterknife.OnEditorAction;

/**
 * App Configuration Activity - settings to change the way the app behaves
 * Note: settings are saved when OK is pressed
 */
public class AppConfigurationActivity extends BaseActivity {

    // VIEWS
    @BindView(R.id.history_linecount) TextInputEditText historyLinecount;
    @BindView(R.id.spinner_timezone) Spinner spinnerTimezone;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_app_configuration;

    // STATE
    private Configuration configuration;
    private ConfigurationManager configurationManager;
    private ArrayAdapter tzAdapter;
    private Handler handler;

    public static Intent getNewIntent(Context context) {
        return new Intent(context, AppConfigurationActivity.class);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);

        handler = new Handler(Looper.getMainLooper());
        configurationManager = new ConfigurationManager(this);
        configuration = configurationManager.getConfiguration();

        initTimezoneAdapter();
        showConfiguration();
    }

    // Register for SpeechService
    @Override
    public void onStart() {
        super.onStart();
        if (speechServiceBinder == null) {
            handler.post(this::doBindService);
        }
    }

    // Unregister SpeechService
    @Override
    public void onStop() {
        if (speechServiceBinder != null) {
            unbindService(myConnection);
            speechServiceBinder = null;
        }
        super.onStop();
    }

    @OnEditorAction({R.id.history_linecount})
    boolean onEditorAction(int actionId, KeyEvent key){
        boolean handled = false;
        if (actionId == EditorInfo.IME_ACTION_SEND || (key != null && key.getKeyCode() == KeyEvent.KEYCODE_ENTER)) {
            hideKeyboardFrom(getCurrentFocus());
            handled = true;
        }
        return handled;
    }

    @OnClick(R.id.send_timezone_btn)
    void onClickSendTz() {
        try {
            speechServiceBinder.sendTimeZoneEvent( (String)tzAdapter.getItem(spinnerTimezone.getSelectedItemPosition()) );
        } catch (RemoteException e) {
            e.printStackTrace();
        }
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

    @OnClick(R.id.btn_cancel)
    public void onClickCancel() {
        finish();
    }

    @OnClick(R.id.btn_ok)
    public void onClickOk() {
        saveConfiguration();// must save updated config first
        finish();
    }

    @Override
    protected void serviceConnected() {
        initializeAndConnect();
    }

    private void showConfiguration(){
        int iHistoryLinecount = configuration.historyLinecount==null?1:configuration.historyLinecount;
        String historyLineCount = String.valueOf(iHistoryLinecount);
        historyLinecount.setText(historyLineCount);

        selectTimezone(configuration.currentTimezone);
    }

    private void saveConfiguration(){
        // history linecount
        configuration.historyLinecount = Integer.valueOf(historyLinecount.getText().toString());
        if (configuration.historyLinecount == 0) configuration.historyLinecount = 1;//do not allow 0

        // timezone
        configuration.currentTimezone = (String)tzAdapter.getItem(spinnerTimezone.getSelectedItemPosition());

        // store data
        configurationManager.setConfiguration(configuration);
    }

}
