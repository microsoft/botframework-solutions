package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.list;

import android.support.annotation.NonNull;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.RecyclerView;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

import java.util.ArrayList;
import java.util.List;

import client.model.BotConnectorActivity;

public class ChatAdapter extends RecyclerView.Adapter<ChatViewholder> {

    // CONSTANTS
    private final int CONTENT_VIEW = R.layout.item_chat;
    private static final String LOGTAG = "ChatAdapter";

    // STATE
    private ArrayList<BotConnectorActivity> chatList = new ArrayList<>();
    private AppCompatActivity parentActivity;
    private static int MAX_CHAT_ITEMS = 1;


    @NonNull
    @Override
    public ChatViewholder onCreateViewHolder(@NonNull ViewGroup parent, int i) {
        View view = LayoutInflater.from(parent.getContext()).inflate(CONTENT_VIEW, parent, false);
        return new ChatViewholder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull ChatViewholder chatViewholder, int position) {
        BotConnectorActivity botConnectorActivity = chatList.get(position);
        chatViewholder.bind(botConnectorActivity, parentActivity);
    }

    @Override
    public int getItemCount() {
        if (chatList == null) return 0;
        return chatList.size();
    }

    public void addChat(BotConnectorActivity botConnectorActivity, AppCompatActivity parentActivity) {
        Log.v(LOGTAG, "showing row id "+ botConnectorActivity.getId());
        this.parentActivity = parentActivity;
        chatList.add(botConnectorActivity);
        if (chatList.size() > MAX_CHAT_ITEMS) {
            chatList.remove(0);
        }
        notifyDataSetChanged();
    }

    public void setChatItemHistoryCount(int count){
        MAX_CHAT_ITEMS = count;
        while (chatList.size() > MAX_CHAT_ITEMS) {
            chatList.remove(0);
        }
        notifyDataSetChanged();
    }

    public void swapChatList(List<BotConnectorActivity> newChatList) {
        chatList.clear();
        chatList.addAll(newChatList);
        notifyDataSetChanged();
    }
}
