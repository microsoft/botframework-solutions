package events;

public class Disconnected {

    public int cancellationReason;
    public String errorDetails;
    public int errorCode;

    public Disconnected(int cancellationReason, String errorDetails, int errorCode) {
        this.cancellationReason = cancellationReason;
        this.errorDetails = errorDetails;
        this.errorCode = errorCode;
    }
}
