package events;

public class GpsLocationSent {
    public String latitude;
    public String longitude;

    public GpsLocationSent(String lat, String lon) {
        latitude = lat;
        longitude = lon;
    }
}
