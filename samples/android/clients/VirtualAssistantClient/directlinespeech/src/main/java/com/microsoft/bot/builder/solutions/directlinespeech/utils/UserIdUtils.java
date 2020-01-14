package com.microsoft.bot.builder.solutions.directlinespeech.utils;

import java.util.UUID;

public class UserIdUtils {
    public static String GenerateUserId() {
        UUID uuid = UUID.randomUUID();
        return "directlinespeech|" + uuid;
    }
}
