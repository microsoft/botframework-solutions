package com.microsoft.bot.builder.solutions.directlinespeech.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class Configuration {

    @SerializedName("service_key")
    @Expose
    public String serviceKey;

    @SerializedName("service_region")
    @Expose
    public String serviceRegion;

    @SerializedName("user_id")
    @Expose
    public String userId;

    @SerializedName("locale")
    @Expose
    public String locale;

    @SerializedName("user_name")
    @Expose
    public String userName;

    @SerializedName("current_timezone")
    @Expose
    public String currentTimezone;//stores the TZ ID

    @SerializedName("keyword")
    @Expose
    public String keyword;

    @SerializedName("enableKWS")
    @Expose
    public Boolean enableKWS;
}
