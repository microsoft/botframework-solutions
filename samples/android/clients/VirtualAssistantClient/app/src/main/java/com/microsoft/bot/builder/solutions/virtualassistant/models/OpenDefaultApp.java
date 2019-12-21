package com.microsoft.bot.builder.solutions.virtualassistant.models;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class OpenDefaultApp {
    @Expose
    @SerializedName("MeetingUri")
    public String meetingUri;

    @Expose
    @SerializedName("TelephoneUri")
    public String telephoneUri;

    @Expose
    @SerializedName("MapsUri")
    public String mapsUri;

    @Expose
    @SerializedName("MusicUri")
    public String musicUri;
}
