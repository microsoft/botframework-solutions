package com.microsoft.directlinechatbot.bo

import java.io.Serializable

public data class ChannelData (val vinNumber: String,
                                 val bmwId: String,
                                 val debug: Boolean,
                                 val firstName: String,
                                 val lastName: String,
                                 val geoLocation: GeoLocation,
                                 val claims: Claims)
    : Serializable