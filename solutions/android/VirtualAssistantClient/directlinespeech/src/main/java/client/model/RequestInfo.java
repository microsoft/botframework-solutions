
package client.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class RequestInfo {

    @SerializedName("interactionId")
    @Expose
    private String interactionId;
    @SerializedName("requestType")
    @Expose
    private Integer requestType;
    @SerializedName("version")
    @Expose
    private String version;

    public String getInteractionId() {
        return interactionId;
    }

    public void setInteractionId(String interactionId) {
        this.interactionId = interactionId;
    }

    public Integer getRequestType() {
        return requestType;
    }

    public void setRequestType(Integer requestType) {
        this.requestType = requestType;
    }

    public String getVersion() {
        return version;
    }

    public void setVersion(String version) {
        this.version = version;
    }

}
