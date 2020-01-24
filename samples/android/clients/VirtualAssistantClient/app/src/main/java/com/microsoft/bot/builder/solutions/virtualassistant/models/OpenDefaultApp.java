package com.microsoft.bot.builder.solutions.virtualassistant.models;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class OpenDefaultApp {
    @Expose
    @SerializedName("meetingUri")
    public String meetingUri;

    @Expose
    @SerializedName("telephoneUri")
    public String telephoneUri;

    @Expose
    @SerializedName("mapsUri")
    public String mapsUri;

    @Expose
    @SerializedName("musicUri")
    public String musicUri;
}
