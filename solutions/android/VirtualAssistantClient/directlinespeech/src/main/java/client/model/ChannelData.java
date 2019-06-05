
package client.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class ChannelData {

    @SerializedName("conversationalAiData")
    @Expose
    private ConversationalAiData conversationalAiData;

    public ConversationalAiData getConversationalAiData() {
        return conversationalAiData;
    }

    public void setConversationalAiData(ConversationalAiData conversationalAiData) {
        this.conversationalAiData = conversationalAiData;
    }

}
