package com.microsoft.bot.builder.solutions.virtualassistant.service;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.location.Location;
import android.os.Looper;
import android.support.v4.app.ActivityCompat;
import android.util.Log;

import com.google.android.gms.common.ConnectionResult;
import com.google.android.gms.common.GoogleApiAvailability;
import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationCallback;
import com.google.android.gms.location.LocationRequest;
import com.google.android.gms.location.LocationResult;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.location.LocationSettingsRequest;
import com.google.android.gms.location.SettingsClient;


public class LocationProvider {

    // CONSTANTS
    private static final int LOCATION_UPDATE_INTERVAL = 3 * 60 * 1000;// 3 mins
    private static final float LOCATION_UPDATE_DISTANCE = 50;// distance (in meters) to move before receiving an update
    private static final String LOGTAG = "LocationProvider";

    // STATE
    private boolean isGettingLocationUpdates;
    private FusedLocationProviderClient fusedLocationClient;
    private LocationCallback locationCallback;
    private Context context;
    private LocationProviderCallback locationProviderCallback;

    // INTERFACE
    public interface LocationProviderCallback {
        void onLocationResult(Location location);
    }

    public LocationProvider(Context context, LocationProviderCallback locationProviderCallback) {
        this.context = context;
        this.locationProviderCallback = locationProviderCallback;
        // check if Google Play Services is installed
        isGettingLocationUpdates = checkPlayServices();
    }

    protected void startLocationUpdates() {
        if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED) {

            fusedLocationClient = LocationServices.getFusedLocationProviderClient(context);

            // register to receive future location updates
            LocationRequest locationRequest = new LocationRequest()
                    .setPriority(LocationRequest.PRIORITY_HIGH_ACCURACY)
                    .setInterval(LOCATION_UPDATE_INTERVAL)
                    .setSmallestDisplacement(LOCATION_UPDATE_DISTANCE);

            // Create LocationSettingsRequest object using location request
            LocationSettingsRequest.Builder builder = new LocationSettingsRequest.Builder();
            builder.addLocationRequest(locationRequest);
            LocationSettingsRequest locationSettingsRequest = builder.build();

            // Check whether location settings are satisfied
            // https://developers.google.com/android/reference/com/google/android/gms/location/SettingsClient
            SettingsClient settingsClient = LocationServices.getSettingsClient(context);
            settingsClient.checkLocationSettings(locationSettingsRequest);
            locationCallback = new LocationCallback() {
                @Override
                public void onLocationResult(LocationResult locationResult) {
                    // Inform the Bot of the location change
                    Location location = locationResult.getLastLocation();
                    if (location != null && locationProviderCallback != null) {
                        locationProviderCallback.onLocationResult(location);
                    }
                }
            };

            fusedLocationClient.requestLocationUpdates(locationRequest, locationCallback, Looper.myLooper());
        } else {
            //this will trigger the first time the app is run - just retry after getting permission
            Log.e(LOGTAG, "Missing ACCESS_FINE_LOCATION permission");
        }
    }

    public void stopLocationUpdates(){
        // stop location updates
        if (isGettingLocationUpdates && fusedLocationClient != null && locationCallback != null) {
            fusedLocationClient.removeLocationUpdates(locationCallback);
        }
    }

    private boolean checkPlayServices() {
        GoogleApiAvailability apiAvailability = GoogleApiAvailability.getInstance();
        int resultCode = apiAvailability.isGooglePlayServicesAvailable(context);
        if (resultCode != ConnectionResult.SUCCESS) {
            if (apiAvailability.isUserResolvableError(resultCode)) {
                apiAvailability.showErrorNotification(context, resultCode);
            }
            return false;
        }
        return true;
    }

}
