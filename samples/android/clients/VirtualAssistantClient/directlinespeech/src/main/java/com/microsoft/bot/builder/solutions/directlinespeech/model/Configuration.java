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

    @SerializedName("custom_commands_app_id")
    @Expose
    public String customCommandsAppId;

    @SerializedName("user_id")
    @Expose
    public String userId;

    @SerializedName("custom_voice_deployment_ids")
    @Expose
    public String customVoiceDeploymentIds;

    @SerializedName("custom_speech_recognition_endpoint_id")
    @Expose
    public String customSpeechRecognitionEndpointId;

    @SerializedName("barge_in_supported")
    @Expose
    public Boolean bargeInSupported;

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

    @SerializedName("signedIn")
    @Expose
    public Boolean signedIn;

    @SerializedName("linkedAccountEndpoint")
    @Expose
    public String linkedAccountEndpoint;
}
