package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.arch.lifecycle.ViewModelProviders;
import android.content.Context;
import android.support.annotation.NonNull;
import android.support.v4.app.FragmentActivity;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.RecyclerView;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

import java.util.ArrayList;
import java.util.stream.Collectors;

import client.model.BotConnectorActivity;

public class ChatAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder> {

    // CONSTANTS
    private static final String LOGTAG = "ChatAdapter";
    private static final int MSG_TYPE_BOT = 1;
    private static final int MSG_TYPE_USER = 2;

    // STATE
    private ArrayList<ChatModel> chatList; // visible chat history
    private ArrayList<ChatModel> chatHistory; // full chat history
    private AppCompatActivity parentActivity;
    private ViewholderBot.OnClickListener clickListener;
    private static int MAX_CHAT_ITEMS = 2;
    private boolean showFullConversation;
    private Integer colorBubbleBot;
    private Integer colorBubbleUser;
    private Integer colorTextBot;
    private Integer colorTextUser;
    private ChatViewModel chatViewModel;

    public ChatAdapter(Context context) {
        // load chat history from view model
        chatViewModel = ViewModelProviders.of((FragmentActivity) context).get(ChatViewModel.class);
        chatHistory = chatViewModel.getChatHistory().getValue();
        showFullConversation = chatViewModel.getShowFullConversation().getValue();

        // filter chat history by the value of showFullConversation
        if (showFullConversation) {
            chatList = new ArrayList<>(chatHistory);
        } else {
            chatList = (ArrayList<ChatModel>) chatHistory.stream().filter(chatModel -> chatModel.userRequest == null).collect(Collectors.toList());
        }
        this.notifyDataSetChanged();
    }

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
            ((ViewholderBot)viewHolder).bind(chatModel, parentActivity, clickListener, colorBubbleBot, colorTextBot);
        }
        if (getItemViewType(position) == MSG_TYPE_USER) {
            ((ViewholderUser)viewHolder).bind(chatModel, parentActivity, colorBubbleUser, colorTextUser);
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
        // only if showFullConversation changed
        if (this.showFullConversation != showFullConversation) {
            this.showFullConversation = showFullConversation;
            chatList.clear();
            // filter chat history by the value of showFullConversation
            if (this.showFullConversation) {
                chatList = new ArrayList<>(chatHistory);
            } else {
                chatList = (ArrayList<ChatModel>) chatHistory.stream().filter(chatModel -> chatModel.userRequest == null).collect(Collectors.toList());
            }
            chatViewModel.setShowFullConversation(showFullConversation);
            notifyDataSetChanged();
        }
    }

    public void addBotResponse(BotConnectorActivity botConnectorActivity, AppCompatActivity parentActivity, ViewholderBot.OnClickListener clickListener) {
        this.parentActivity = parentActivity;
        this.clickListener = clickListener;
        ChatModel chatModel = new ChatModel(botConnectorActivity);
        chatHistory.add(chatModel);
        chatViewModel.setChatHistory(chatHistory);
        chatList.add(chatModel);
        if (chatList.size() > MAX_CHAT_ITEMS) {
            chatList.remove(0);
        }
        notifyDataSetChanged();
    }

    public void addUserRequest(String request) {
        ChatModel chatModel = new ChatModel(request);
        chatHistory.add(chatModel);
        chatViewModel.setChatHistory(chatHistory);
        if (showFullConversation) {
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

    public void resetChat(){
        chatList.clear();
        chatHistory.clear();
        chatViewModel.setChatHistory(chatHistory);
        notifyDataSetChanged();
    }

    public void setChatBubbleColors(Integer colorBubbleBot, Integer colorBubbleUser){
        this.colorBubbleBot = colorBubbleBot;
        this.colorBubbleUser = colorBubbleUser;
    }

    public void setChatTextColors(Integer colorTextBot, Integer colorTextUser){
        this.colorTextBot = colorTextBot;
        this.colorTextUser = colorTextUser;
    }

}
