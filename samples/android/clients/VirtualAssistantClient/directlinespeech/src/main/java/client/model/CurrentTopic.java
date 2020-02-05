package client.model;

import java.io.Serializable;
import java.util.List;
import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class CurrentTopic implements Serializable
{

    @SerializedName("DonePredicates")
    @Expose
    private String donePredicates;
    @SerializedName("Intent")
    @Expose
    private String intent;
    @SerializedName("PossibleIntents")
    @Expose
    private List<Object> possibleIntents = null;
    private final static long serialVersionUID = 1572967746378672776L;

    public String getDonePredicates() {
        return donePredicates;
    }

    public void setDonePredicates(String donePredicates) {
        this.donePredicates = donePredicates;
    }

    public String getIntent() {
        return intent;
    }

    public void setIntent(String intent) {
        this.intent = intent;
    }

    public List<Object> getPossibleIntents() {
        return possibleIntents;
    }

    public void setPossibleIntents(List<Object> possibleIntents) {
        this.possibleIntents = possibleIntents;
    }

}