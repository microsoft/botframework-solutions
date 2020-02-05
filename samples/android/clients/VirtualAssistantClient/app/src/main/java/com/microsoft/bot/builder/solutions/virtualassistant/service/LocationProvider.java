package com.microsoft.bot.builder.solutions.virtualassistant.service;

import android.Manifest;
import android.content.Context;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Bundle;
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
    private boolean isPlayStoreInstalled;
    private FusedLocationProviderClient fusedLocationClient;// dependency on Play Store
    private LocationCallback locationCallback;
    private Context context;
    private LocationProviderCallback locationProviderCallback;
    private LocationListener locationListener;
    private Location lastKnownLocation;

    // INTERFACE
    public interface LocationProviderCallback {
        void onLocationResult(Location location);
    }

    public LocationProvider(Context context, LocationProviderCallback locationProviderCallback) {
        this.context = context;
        this.locationProviderCallback = locationProviderCallback;
        // check if Google Play Services is installed
        isPlayStoreInstalled = checkPlayServices();
    }

    protected Location getLastKnownLocation() {
        return lastKnownLocation;
    }

    /**
     * Start receiving periodic location updates
     */
    protected void startLocationUpdates() {
        if (isPlayStoreInstalled) {
            if (fusedLocationClient == null) {
                if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED) {

                    fusedLocationClient = LocationServices.getFusedLocationProviderClient(context);

                    // register to receive future location updates
                    LocationRequest locationRequest = new LocationRequest()
                            .setPriority(LocationRequest.PRIORITY_BALANCED_POWER_ACCURACY)
                            .setInterval(LOCATION_UPDATE_INTERVAL);

                    // If the device doesn't move it prevents location callbacks - not good for dev't
                    boolean isDebug = ((context.getApplicationInfo().flags & ApplicationInfo.FLAG_DEBUGGABLE) != 0);
                    if (!isDebug) locationRequest.setSmallestDisplacement(LOCATION_UPDATE_DISTANCE);

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
                            lastKnownLocation = location;
                            if (location != null && locationProviderCallback != null) {
                                locationProviderCallback.onLocationResult(location);
                            }
                        }
                    };

                    fusedLocationClient.requestLocationUpdates(locationRequest, locationCallback, Looper.getMainLooper());
                } else {
                    //this will trigger the first time the app is run - just retry after getting permission
                    Log.i(LOGTAG, "Missing ACCESS_FINE_LOCATION permission");
                }
            }
        } else {
            locationUpdatesWithoutGoogleServices();
        }
    }

    public void stopLocationUpdates(){
        // stop location updates
        if (isPlayStoreInstalled && fusedLocationClient != null && locationCallback != null) {
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

    private void locationUpdatesWithoutGoogleServices(){
        if (locationListener == null) {
            if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED) {

                locationListener = getLocationListener();
                LocationManager locationManager = (LocationManager) context.getSystemService(Context.LOCATION_SERVICE);
                boolean isDebug = ((context.getApplicationInfo().flags & ApplicationInfo.FLAG_DEBUGGABLE) != 0);

                if (isDebug) {
                    // do not request minDistance when developing
                    locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, LOCATION_UPDATE_INTERVAL, 0, locationListener);
                } else {
                    locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, LOCATION_UPDATE_INTERVAL, LOCATION_UPDATE_DISTANCE, locationListener);
                }
            }
        }
    }

    private LocationListener getLocationListener(){
        return new LocationListener() {
            @Override
            public void onLocationChanged(Location location) {
                lastKnownLocation = location;
                if (location != null && locationProviderCallback != null) {
                    locationProviderCallback.onLocationResult(location);
                }
            }

            @Override
            public void onStatusChanged(String provider, int status, Bundle extras) {

            }

            @Override
            public void onProviderEnabled(String provider) {

            }

            @Override
            public void onProviderDisabled(String provider) {

            }
        };
    }

}
