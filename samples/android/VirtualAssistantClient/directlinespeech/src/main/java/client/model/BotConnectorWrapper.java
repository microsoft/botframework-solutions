
package client.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class BotConnectorWrapper {

    @SerializedName("conversationId")
    @Expose
    private String conversationId;


    @SerializedName("messagePayload")
    @Expose
    private BotConnectorActivity messagePayload;


    @SerializedName("version")
    @Expose
    private Double version;

    public String getConversationId() {
        return conversationId;
    }

    public void setConversationId(String conversationId) {
        this.conversationId = conversationId;
    }

    public BotConnectorActivity getMessagePayload() {
        return messagePayload;
    }

    public void setMessagePayload(BotConnectorActivity messagePayload) {
        this.messagePayload = messagePayload;
    }

    public Double getVersion() {
        return version;
    }

    public void setVersion(Double version) {
        this.version = version;
    }

}
