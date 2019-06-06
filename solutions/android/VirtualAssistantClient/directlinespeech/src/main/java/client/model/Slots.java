package client.model;

import java.io.Serializable;
import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class Slots implements Serializable
{

    @SerializedName("CurrentIntent")
    @Expose
    private String currentIntent;
    @SerializedName("LastUtterance")
    @Expose
    private String lastUtterance;
    private final static long serialVersionUID = -1057796387188213661L;

    public String getCurrentIntent() {
        return currentIntent;
    }

    public void setCurrentIntent(String currentIntent) {
        this.currentIntent = currentIntent;
    }

    public String getLastUtterance() {
        return lastUtterance;
    }

    public void setLastUtterance(String lastUtterance) {
        this.lastUtterance = lastUtterance;
    }

}