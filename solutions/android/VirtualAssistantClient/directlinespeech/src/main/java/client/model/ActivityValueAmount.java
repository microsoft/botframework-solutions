package client.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class ActivityValueAmount {

    @SerializedName("Amount")
    @Expose
    private Integer amount;

    @SerializedName("Unit")
    @Expose
    private String unit;

    public Integer getAmount() {
        return amount;
    }

    public void setAmount(Integer amount) {
        this.amount = amount;
    }

    public String getUnit() {
        return unit;
    }

    public void setUnit(String unit) {
        this.unit = unit;
    }
}
