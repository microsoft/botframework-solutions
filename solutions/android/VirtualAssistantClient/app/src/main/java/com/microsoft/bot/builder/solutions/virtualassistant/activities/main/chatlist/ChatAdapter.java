package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.support.annotation.NonNull;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.RecyclerView;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

import java.util.ArrayList;

import client.model.BotConnectorActivity;

public class ChatAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder> {

    // CONSTANTS
    private static final String LOGTAG = "ChatAdapter";
    private static final int MSG_TYPE_BOT = 1;
    private static final int MSG_TYPE_USER = 2;

    // STATE
    private ArrayList<ChatModel> chatList = new ArrayList<>();
    private AppCompatActivity parentActivity;
    private ViewholderBot.OnClickListener clickListener;
    private static int MAX_CHAT_ITEMS = 2;
    private boolean showFullConversation;


    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        RecyclerView.ViewHolder viewHolder = null;

        if (viewType == MSG_TYPE_BOT) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_chat_bot, parent, false);
            viewHolder = new ViewholderBot(view);
        }
        if (viewType == MSG_TYPE_USER) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_chat_user, parent, false);
            viewHolder = new ViewholderUser(view);
        }

        return viewHolder;
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder viewHolder, int position) {
        ChatModel chatModel = chatList.get(position);
        if (getItemViewType(position) == MSG_TYPE_BOT) {
            ((ViewholderBot)viewHolder).bind(chatModel, parentActivity, clickListener);
        }
        if (getItemViewType(position) == MSG_TYPE_USER) {
            ((ViewholderUser)viewHolder).bind(chatModel, parentActivity);
        }
    }

    @Override
    public int getItemViewType(int position) {
        ChatModel chatModel = chatList.get(position);
        if (chatModel.userRequest != null)
            return MSG_TYPE_USER;
        else
            return MSG_TYPE_BOT;
    }

    @Override
    public int getItemCount() {
        if (chatList == null) return 0;
        return chatList.size();
    }

    public void setShowFullConversation(boolean showFullConversation){
        this.showFullConversation = showFullConversation;
        chatList.clear();
        notifyDataSetChanged();
    }

    public void addBotResponse(BotConnectorActivity botConnectorActivity, AppCompatActivity parentActivity, ViewholderBot.OnClickListener clickListener) {
        Log.v(LOGTAG, "showing row id "+ botConnectorActivity.getId());
        this.parentActivity = parentActivity;
        this.clickListener = clickListener;
        ChatModel chatModel = new ChatModel(botConnectorActivity);
        chatList.add(chatModel);
        if (chatList.size() > MAX_CHAT_ITEMS) {
            chatList.remove(0);
        }
        notifyDataSetChanged();
    }

    public void addUserRequest(String request) {
        if (showFullConversation) {
            ChatModel chatModel = new ChatModel(request);
            chatList.add(chatModel);
            if (chatList.size() > MAX_CHAT_ITEMS) {
                chatList.remove(0);
            }
            notifyDataSetChanged();
        }
    }

    public void setChatItemHistoryCount(int count){
        MAX_CHAT_ITEMS = count;
        while (chatList.size() > MAX_CHAT_ITEMS) {
            chatList.remove(0);
        }
        notifyDataSetChanged();
    }
}
