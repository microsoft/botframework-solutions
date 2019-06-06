package client.model;

import java.io.Serializable;
import java.util.List;
import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class ActivityValue implements Serializable
{

    @SerializedName("ActiveTopics")
    @Expose
    private List<String> activeTopics = null;
    @SerializedName("CurrentTopic")
    @Expose
    private CurrentTopic currentTopic;
    @SerializedName("Slots")
    @Expose
    private Slots slots;
    private final static long serialVersionUID = 8137697838200767049L;

    public List<String> getActiveTopics() {
        return activeTopics;
    }

    public void setActiveTopics(List<String> activeTopics) {
        this.activeTopics = activeTopics;
    }

    public CurrentTopic getCurrentTopic() {
        return currentTopic;
    }

    public void setCurrentTopic(CurrentTopic currentTopic) {
        this.currentTopic = currentTopic;
    }

    public Slots getSlots() {
        return slots;
    }

    public void setSlots(Slots slots) {
        this.slots = slots;
    }

}