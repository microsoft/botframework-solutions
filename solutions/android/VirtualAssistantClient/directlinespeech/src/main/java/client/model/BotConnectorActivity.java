
package client.model;

import java.util.List;
import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class BotConnectorActivity {

    @SerializedName("attachmentLayout")
    @Expose
    private String attachmentLayout;

    public String getAttachmentLayout() {
        return attachmentLayout;
    }

    public void setAttachmentLayout(String attachmentLayout) {
        this.attachmentLayout = attachmentLayout;
    }

    @SerializedName("attachments")
    @Expose
    private List<Object> attachments = null;

    @SerializedName("channelData")
    @Expose
    private ChannelData channelData;
    @SerializedName("channelId")
    @Expose
    private String channelId;
    @SerializedName("conversation")
    @Expose
    private ConversationBot conversation;
    @SerializedName("entities")
    @Expose
    private List<Object> entities = null;
    @SerializedName("from")
    @Expose
    private From from;
    @SerializedName("id")
    @Expose
    private String id;
    @SerializedName("inputHint")
    @Expose
    private String inputHint;
    @SerializedName("locale")
    @Expose
    private String locale;
    @SerializedName("recipient")
    @Expose
    private Recipient recipient;
    @SerializedName("replyToId")
    @Expose
    private String replyToId;
    @SerializedName("serviceUrl")
    @Expose
    private String serviceUrl;
    @SerializedName("speak")
    @Expose
    private String speak;
    @SerializedName("text")
    @Expose
    private String text;
    @SerializedName("timestamp")
    @Expose
    private String timestamp;
    @SerializedName("type")
    @Expose
    private String type;
    @SerializedName("File")
    @Expose
    private String file;
    @SerializedName("value")
    @Expose
    private ActivityValue value;
    @SerializedName("Amount")
    @Expose
    private String amount;
    @SerializedName("requestedState")
    @Expose
    private String requestedState;
    @SerializedName("seat")
    @Expose
    private String seat;

    public List<Object> getAttachments() {
        return attachments;
    }

    public void setAttachments(List<Object> attachments) {
        this.attachments = attachments;
    }

    public ChannelData getChannelData() {
        return channelData;
    }

    public void setChannelData(ChannelData channelData) {
        this.channelData = channelData;
    }

    public String getChannelId() {
        return channelId;
    }

    public void setChannelId(String channelId) {
        this.channelId = channelId;
    }

    public ConversationBot getConversation() {
        return conversation;
    }

    public void setConversation(ConversationBot conversation) {
        this.conversation = conversation;
    }

    public List<Object> getEntities() {
        return entities;
    }

    public void setEntities(List<Object> entities) {
        this.entities = entities;
    }

    public From getFrom() {
        return from;
    }

    public void setFrom(From from) {
        this.from = from;
    }

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getInputHint() {
        return inputHint;
    }

    public void setInputHint(String inputHint) {
        this.inputHint = inputHint;
    }

    public String getLocale() {
        return locale;
    }

    public void setLocale(String locale) {
        this.locale = locale;
    }

    public Recipient getRecipient() {
        return recipient;
    }

    public void setRecipient(Recipient recipient) {
        this.recipient = recipient;
    }

    public String getReplyToId() {
        return replyToId;
    }

    public void setReplyToId(String replyToId) {
        this.replyToId = replyToId;
    }

    public String getServiceUrl() {
        return serviceUrl;
    }

    public void setServiceUrl(String serviceUrl) {
        this.serviceUrl = serviceUrl;
    }

    public String getSpeak() {
        return speak;
    }

    public void setSpeak(String speak) {
        this.speak = speak;
    }

    public String getText() {
        return text;
    }

    public void setText(String text) {
        this.text = text;
    }

    public String getTimestamp() {
        return timestamp;
    }

    public void setTimestamp(String timestamp) {
        this.timestamp = timestamp;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public ActivityValue getValue() {
        return value;
    }

    public void setValue(ActivityValue value) {
        this.value = value;
    }

    public String getFile() {
        return file;
    }

    public void setFile(String file) {
        this.file = file;
    }

    public String getAmount() {
        return amount;
    }

    public void setAmount(String amount) {
        this.amount = amount;
    }

    public String getRequestedState() { return this.requestedState;}

    public void setRequestedState(String requestedState) { this.requestedState = requestedState;}

    public void setSeat(String seat) { this.seat = seat;}

    public String getSeat() { return seat;}
}
