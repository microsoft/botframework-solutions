package events;

public class Disconnected {

    public String errorDetails;
    public int errorCode;

    public Disconnected(String errorDetails, int errorCode) {
        this.errorDetails = errorDetails;
        this.errorCode = errorCode;
    }
}
