package com.microsoft.bot.builder.solutions.virtualassistant.activities;

import android.Manifest;
import android.app.Activity;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.os.IBinder;
import android.os.RemoteException;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.design.widget.Snackbar;
import android.support.v4.app.ActivityCompat;
import android.support.v7.app.AlertDialog;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;
import android.view.View;
import android.view.inputmethod.InputMethodManager;
import android.widget.Toast;

import com.microsoft.bot.builder.solutions.directlinespeech.ConfigurationManager;
import com.microsoft.bot.builder.solutions.directlinespeech.model.Configuration;
import com.microsoft.bot.builder.solutions.virtualassistant.ISpeechService;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.utils.AppConfigurationManager;

/**
 * This base class provides functionality that is reusable in Activities of this app
 */
public abstract class BaseActivity extends AppCompatActivity {

    // CONSTANTS
    public static final String LOGTAG = "BaseActivity";
    private static final Integer PERMISSION_REQUEST_RECORD_AUDIO = 101;
    private static final Integer PERMISSION_REQUEST_FINE_LOCATION = 102;
    public static final String SHARED_PREFS_NAME = "my_shared_prefs";

    // STATE
    private SharedPreferences sharedPreferences;
    protected ISpeechService speechServiceBinder;
    protected ConfigurationManager configurationManager;
    protected AppConfigurationManager appConfigurationManager;

    // OVERRIDE THESE
    protected void permissionDenied(String manifestPermission){}
    protected void permissionGranted(String manifestPermission){}
    protected void serviceConnected(){}

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        sharedPreferences = getSharedPreferences(SHARED_PREFS_NAME, MODE_PRIVATE);
        setupMainWindowDisplayMode();
        configurationManager = new ConfigurationManager(this);
        appConfigurationManager = new AppConfigurationManager(this);
    }

    @Override
    protected void onResume() {
        super.onResume();
        setSystemUiVisilityMode();
    }

    private void setupMainWindowDisplayMode() {
        View decorView = setSystemUiVisilityMode();
        decorView.setOnSystemUiVisibilityChangeListener(visibility -> {
            setSystemUiVisilityMode(); // Needed to avoid exiting immersive_sticky when keyboard is displayed
        });
    }

    private View setSystemUiVisilityMode() {
        View decorView = getWindow().getDecorView();
        int options;
        options =
                View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                        | View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                        | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                        | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION // hide nav bar
                        | View.SYSTEM_UI_FLAG_FULLSCREEN // hide status bar
                        | View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY;

        decorView.setSystemUiVisibility(options);
        return decorView;
    }

    protected void hideKeyboardFrom(View view) {
        InputMethodManager imm = (InputMethodManager) getSystemService(Activity.INPUT_METHOD_SERVICE);
        imm.hideSoftInputFromWindow(view.getWindowToken(), 0);
    }

    protected void showSnackbar(View view, String message){
        Snackbar.make(view, message, Snackbar.LENGTH_SHORT)
                .setAction("Action", null)
                .show();
    }

    protected void showToast(String message){
        Toast.makeText(getApplicationContext(), message, Toast.LENGTH_LONG).show();
    }

    protected void putBooleanSharedPref(String prefName, boolean value){
        SharedPreferences.Editor editor = sharedPreferences.edit();
        editor.putBoolean(prefName, value);
        editor.commit();
    }

    protected boolean getBooleanSharedPref(String prefName) {
        return sharedPreferences.getBoolean(prefName, false);
    }

    /**
     * Requests the RECORD_AUDIO and WRITE_EXTERNAL_STORAGE permissions as they are "Dangerous"
     * If the permission has been denied previously, a dialog with extra rationale info will prompt
     * the user to grant the permission, otherwise it is requested directly.
     */
    protected void requestRecordAudioPermissions() {
        if (ActivityCompat.shouldShowRequestPermissionRationale(this, Manifest.permission.RECORD_AUDIO)) {
            // Provide an additional rationale to the user if the permission was not granted
            // and the user would benefit from additional context for the use of the permission.
            // For example if the user has previously denied the permission.
            new AlertDialog.Builder(this)
                    .setTitle(getString(R.string.permission_record_audio_title))
                    .setMessage(getString(R.string.permission_record_audio_rationale))
                    .setCancelable(false)
                    .setPositiveButton(android.R.string.yes, (dialog, which) -> {
                        //re-request
                        ActivityCompat.requestPermissions(this,
                                new String[]{Manifest.permission.RECORD_AUDIO},
                                PERMISSION_REQUEST_RECORD_AUDIO);
                    })
                    .show();
        } else {
            // RECORD_AUDIO permission has not been granted yet. Request it directly.
            ActivityCompat.requestPermissions(this,
                    new String[]{Manifest.permission.RECORD_AUDIO},
                    PERMISSION_REQUEST_RECORD_AUDIO);
        }
    }

    protected void requestFineLocationPermissions() {
        if (ActivityCompat.shouldShowRequestPermissionRationale(this, Manifest.permission.ACCESS_FINE_LOCATION)) {
            // Provide an additional rationale to the user if the permission was not granted
            // and the user would benefit from additional context for the use of the permission.
            // For example if the user has previously denied the permission.
            new AlertDialog.Builder(this)
                    .setTitle(getString(R.string.permission_access_fine_location_title))
                    .setMessage(getString(R.string.permission_access_fine_location_rationale))
                    .setCancelable(false)
                    .setPositiveButton(android.R.string.yes, (dialog, which) -> {
                        //re-request
                        ActivityCompat.requestPermissions(this,
                                new String[]{Manifest.permission.ACCESS_FINE_LOCATION},
                                PERMISSION_REQUEST_FINE_LOCATION);
                    })
                    .show();
        } else {
            // ACCESS_FINE_LOCATION permission has not been granted yet. Request it directly.
            ActivityCompat.requestPermissions(this,
                    new String[]{Manifest.permission.ACCESS_FINE_LOCATION},
                    PERMISSION_REQUEST_FINE_LOCATION);
        }
    }

    /**
     * Callback received when a permissions request has been completed.
     */
    @Override
    public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions, @NonNull int[] grantResults) {

        // Received permission result for RECORD_AUDIO permission");
        if (requestCode == PERMISSION_REQUEST_RECORD_AUDIO) {
            if (grantResults.length == 1 && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                // RECORD_AUDIO permission has been granted
                permissionGranted(Manifest.permission.RECORD_AUDIO);
            }
            else {
                permissionDenied(Manifest.permission.RECORD_AUDIO);
            }
        }

        // Received permission result for ACCESS_FINE_LOCATION permission");
        if (requestCode == PERMISSION_REQUEST_FINE_LOCATION) {
            if (grantResults.length == 1 && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                // permission has been granted
                permissionGranted(Manifest.permission.ACCESS_FINE_LOCATION);
            }
            else {
                permissionDenied(Manifest.permission.ACCESS_FINE_LOCATION);
            }
        }
    }

    public void doBindService() {
        Intent intent = new Intent();
        intent.setClassName("com.microsoft.bot.builder.solutions.virtualassistant","com.microsoft.bot.builder.solutions.virtualassistant.service.SpeechService");
        boolean success = bindService(intent, myConnection, Context.BIND_AUTO_CREATE);
        Log.d(LOGTAG,"doBindService() success = "+success);
    }

    public ServiceConnection myConnection = new ServiceConnection() {

        public void onServiceConnected(ComponentName className, IBinder binder) {
            speechServiceBinder = ISpeechService.Stub.asInterface(binder);
            Log.d(LOGTAG,"connected");
            // now use speechServiceBinder to execute methods in the service
            serviceConnected();
        }

        public void onServiceDisconnected(ComponentName className) {
            Log.d(LOGTAG,"disconnected");
            speechServiceBinder = null;
        }
    };

    protected void initializeAndConnect(){
        if (speechServiceBinder != null) {
            try {
                boolean havePermission = ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED;
                speechServiceBinder.initializeSpeechSdk(havePermission);
                speechServiceBinder.connectAsync();
            } catch (RemoteException exception){
                Log.e(LOGTAG, exception.getMessage());
            }
        } else {
            Log.e(LOGTAG, "do not have a binding to the service");
        }
    }

    /**
     * enable or disable Keyword Sensing
     * @param enable true to enable
     * @return true if KWS is enabled, otherwise false
     */
    protected boolean setKwsState(boolean enable) {
        boolean enabledKws = false;

        if (enable) {
            if ((ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED)){
                Log.w(LOGTAG, "Insufficient permissions to access audio. Skipping KWS toggle operation");
                return false;
            }

            try {
                final Configuration configuration = configurationManager.getConfiguration();
                String keyword = configuration.keyword;
                if (keyword != null) {
                    speechServiceBinder.startKeywordListeningAsync(keyword);
                }
                enabledKws = true;
            }
            catch (RemoteException e){
                Log.e(LOGTAG, e.getMessage());
            }
        }
        else {
            try {
                speechServiceBinder.stopKeywordListening();
            } catch (RemoteException e) {
                e.printStackTrace();
            }
        }

        return enabledKws;
    }

}
