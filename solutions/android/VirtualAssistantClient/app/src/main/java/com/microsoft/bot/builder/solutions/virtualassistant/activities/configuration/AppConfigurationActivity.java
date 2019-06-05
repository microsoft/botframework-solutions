package com.microsoft.bot.builder.solutions.virtualassistant.activities.configuration;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.support.design.widget.TextInputEditText;
import android.view.KeyEvent;
import android.view.inputmethod.EditorInfo;

import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;

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

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_app_configuration;

    // STATE
    private Configuration configuration;
    private ConfigurationManager configurationManager;

    public static Intent getNewIntent(Context context) {
        return new Intent(context, AppConfigurationActivity.class);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);

        configurationManager = new ConfigurationManager(this);
        configuration = configurationManager.getConfiguration();

        showConfiguration();
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

    @OnClick(R.id.btn_cancel)
    public void onClickCancel() {
        finish();
    }

    @OnClick(R.id.btn_ok)
    public void onClickOk() {
        saveConfiguration();// must save updated config first
        finish();
    }

    private void showConfiguration(){
        int iHistoryLinecount = configuration.historyLinecount==null?1:configuration.historyLinecount;
        String historyLineCount = String.valueOf(iHistoryLinecount);
        historyLinecount.setText(historyLineCount);
    }

    private void saveConfiguration(){
        configuration.historyLinecount = Integer.valueOf(historyLinecount.getText().toString());
        if (configuration.historyLinecount == 0) configuration.historyLinecount = 1;//do not allow 0
        configurationManager.setConfiguration(configuration);
    }

}
