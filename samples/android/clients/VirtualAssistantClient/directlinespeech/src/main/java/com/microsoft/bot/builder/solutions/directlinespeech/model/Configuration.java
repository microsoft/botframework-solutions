package com.microsoft.bot.builder.solutions.directlinespeech.model;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class Configuration {

    @SerializedName("SpeechSubscriptionKey")
    @Expose
    public String speechSubscriptionKey;

    @SerializedName("SpeechRegion")
    @Expose
    public String speechRegion;

    @SerializedName("CustomCommandsAppId")
    @Expose
    public String customCommandsAppId;

    @SerializedName("UserId")
    @Expose
    public String userId;

    @SerializedName("CustomVoiceDeploymentIds")
    @Expose
    public String customVoiceDeploymentIds;

    @SerializedName("CustomSREndpointId")
    @Expose
    public String customSREndpointId;

    @SerializedName("SpeechSDKLogEnabled")
    @Expose
    public Boolean speechSdkLogEnabled;

    @SerializedName("TTSBargeInSupported")
    @Expose
    public Boolean ttsBargeInSupported;

    @SerializedName("SRLanguage")
    @Expose
    public String srLanguage;

    @SerializedName("UserName")
    @Expose
    public String userName;

    @SerializedName("CurrentTimezone")
    @Expose
    public String currentTimezone;//stores the TZ ID

    @SerializedName("Keyword")
    @Expose
    public String keyword;

    @SerializedName("EnableKWS")
    @Expose
    public Boolean enableKWS;

    @SerializedName("SignedIn")
    @Expose
    public Boolean signedIn;

    @SerializedName("LinkedAccountEndpoint")
    @Expose
    public String linkedAccountEndpoint;
}
