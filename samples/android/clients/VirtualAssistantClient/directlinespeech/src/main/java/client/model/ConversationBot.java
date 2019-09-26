package client.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class ConversationBot {

    @SerializedName("id")
    @Expose
    private String id;
    @SerializedName("isGroup")
    @Expose
    private Boolean isGroup;

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public Boolean getIsGroup() {
        return isGroup;
    }

    public void setIsGroup(Boolean isGroup) {
        this.isGroup = isGroup;
    }

}