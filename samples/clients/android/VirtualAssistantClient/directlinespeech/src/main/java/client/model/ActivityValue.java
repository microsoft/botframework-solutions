package client.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class ActivityValue {
    @SerializedName("IsConfirmed")
    @Expose
    private Boolean isConfirmed;

    @SerializedName("IsRelativeAmount")
    @Expose
    private Boolean isRelativeAmount;

    @SerializedName("OperationStatus")
    @Expose
    private Integer operationStatus;

    @SerializedName("SettingName")
    @Expose
    private String settingName;

    @SerializedName("Value")
    @Expose
    private String value;

    @SerializedName("Amount")
    @Expose
    private ActivityValueAmount amount;

    @SerializedName("Uri")
    @Expose
    private String uri;

    public Boolean getConfirmed() {
        return isConfirmed;
    }

    public void setConfirmed(Boolean confirmed) {
        isConfirmed = confirmed;
    }

    public Boolean getRelativeAmount() {
        return isRelativeAmount;
    }

    public void setRelativeAmount(Boolean relativeAmount) {
        isRelativeAmount = relativeAmount;
    }

    public Integer getOperationStatus() {
        return operationStatus;
    }

    public void setOperationStatus(Integer operationStatus) {
        this.operationStatus = operationStatus;
    }

    public String getSettingName() {
        return settingName;
    }

    public void setSettingName(String settingName) {
        this.settingName = settingName;
    }

    public String getValue() {
        return value;
    }

    public void setValue(String value) {
        this.value = value;
    }

    public ActivityValueAmount getAmount() {
        return amount;
    }

    public void setAmount(ActivityValueAmount amount) {
        this.amount = amount;
    }

    public String getUri() {
        return uri;
    }

    public void setUri(String uri) {
        this.uri = uri;
    }
}