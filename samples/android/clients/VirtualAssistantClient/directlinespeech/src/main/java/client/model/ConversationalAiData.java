
package client.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class ConversationalAiData {

    @SerializedName("requestInfo")
    @Expose
    private RequestInfo requestInfo;

    public RequestInfo getRequestInfo() {
        return requestInfo;
    }

    public void setRequestInfo(RequestInfo requestInfo) {
        this.requestInfo = requestInfo;
    }

}
