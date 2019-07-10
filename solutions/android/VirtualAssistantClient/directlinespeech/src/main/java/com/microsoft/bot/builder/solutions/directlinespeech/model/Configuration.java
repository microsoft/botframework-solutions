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

    @SerializedName("bot_id")
    @Expose
    public String botId;

    @SerializedName("user_id")
    @Expose
    public String userId;

    @SerializedName("locale")
    @Expose
    public String locale;

    @SerializedName("geolat")
    @Expose
    public String geolat;

    @SerializedName("geolon")
    @Expose
    public String geolon;

    @SerializedName("user_name")
    @Expose
    public String userName;

    @SerializedName("history_linecount")
    @Expose
    public Integer historyLinecount;

    @SerializedName("current_timezone")
    @Expose
    public String currentTimezone;//stores the TZ ID


    public boolean isEmpty(){
        return serviceKey==null&&
                serviceRegion==null&&
                botId==null&&
                userId==null&&
                locale==null&&
                geolat==null&&
                geolon==null&&
                userName==null&&
                historyLinecount==null&&
                currentTimezone==null;
    }
}
