package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.arch.lifecycle.LiveData;
import android.arch.lifecycle.MutableLiveData;
import android.arch.lifecycle.ViewModel;

import java.util.ArrayList;

public class ChatViewModel extends ViewModel {
    private MutableLiveData<ArrayList<ChatModel>> chatHistory;
    private MutableLiveData<Boolean> showFullConversation;

    public LiveData<ArrayList<ChatModel>> getChatHistory() {
        if (chatHistory == null) {
            chatHistory = new MutableLiveData<>();
            chatHistory.setValue(new ArrayList<>());
        }
        return chatHistory;
    }

    public void setChatHistory(ArrayList<ChatModel> chatHistory) {
        this.chatHistory.setValue(chatHistory);
    }

    public LiveData<Boolean> getShowFullConversation() {
        if (showFullConversation == null) {
            showFullConversation = new MutableLiveData<>();
            showFullConversation.setValue(false);
        }
        return showFullConversation;
    }

    public void setShowFullConversation(Boolean showFullConversation) {
        this.showFullConversation.setValue(showFullConversation);
    }
}
