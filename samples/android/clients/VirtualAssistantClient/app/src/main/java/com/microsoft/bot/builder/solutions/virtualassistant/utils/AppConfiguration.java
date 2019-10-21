package com.microsoft.bot.builder.solutions.virtualassistant.utils;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class AppConfiguration {

    @SerializedName("history_linecount")
    @Expose
    public Integer historyLinecount;

    @SerializedName("color_bubble_bot")
    @Expose
    public Integer colorBubbleBot;

    @SerializedName("color_bubble_user")
    @Expose
    public Integer colorBubbleUser;

    @SerializedName("color_text_bot")
    @Expose
    public Integer colorTextBot;

    @SerializedName("color_text_user")
    @Expose
    public Integer colorTextUser;

    @SerializedName("show_full_conversation")
    @Expose
    public Boolean showFullConversation;

    @SerializedName("enable_dark_mode")
    @Expose
    public Boolean enableDarkMode;

    @SerializedName("keep_screen_on")
    @Expose
    public Boolean keepScreenOn;

    @SerializedName("app_center_id")
    @Expose
    public String appCenterId;
}
