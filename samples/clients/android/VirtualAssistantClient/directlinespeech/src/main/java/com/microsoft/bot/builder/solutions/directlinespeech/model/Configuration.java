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

    @SerializedName("user_name")
    @Expose
    public String userName;

    @SerializedName("history_linecount")
    @Expose
    public Integer historyLinecount;

    @SerializedName("current_timezone")
    @Expose
    public String currentTimezone;//stores the TZ ID

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

    @SerializedName("keyword")
    @Expose
    public String keyword;

    @SerializedName("show_full_conversation")
    @Expose
    public Boolean showFullConversation;

    @SerializedName("enable_dark_mode")
    @Expose
    public Boolean enableDarkMode;

    public boolean isEmpty(){
        return serviceKey==null&&
                serviceRegion==null&&
                botId==null&&
                userId==null&&
                locale==null&&
                userName==null&&
                historyLinecount==null&&
                currentTimezone==null&&
                colorBubbleBot==null&&
                colorBubbleUser==null&&
                colorTextBot==null&&
                colorTextUser==null&&
                keyword==null&&
                showFullConversation==null&&
                enableDarkMode==null;
    }
}
