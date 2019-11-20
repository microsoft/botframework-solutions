package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.support.annotation.NonNull;
import android.support.v7.widget.CardView;
import android.support.v7.widget.RecyclerView;
import android.view.View;
import android.widget.TextView;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

import butterknife.BindView;
import butterknife.ButterKnife;
import client.model.BotConnectorActivity;

public class ViewHolderBot extends RecyclerView.ViewHolder {

    // CONSTANTS
    private static final String LOGTAG = "ChatViewholder";

    // VIEWS
    @BindView(R.id.tv_chat) TextView textMessage;
    @BindView(R.id.bot_message) CardView botMessage;

    // STATE
    private View view;

    public ViewHolderBot(@NonNull View itemView) {
        super(itemView);
        view = itemView;
        ButterKnife.bind(this, view);
    }

    /**
     * bind the layout with the data
     */
    void bind(@NonNull ChatModel chatModel, Integer botBubbleCol, Integer botTextCol) {
        BotConnectorActivity botConnectorActivity = chatModel.botConnectorActivity;
        String message = botConnectorActivity.getText();
        if (message == null || message.isEmpty()) {
            botMessage.setVisibility(View.GONE);
        } else {
            textMessage.setText(botConnectorActivity.getText());
            if (botBubbleCol != null) botMessage.setCardBackgroundColor(botBubbleCol);
            if (botTextCol != null) textMessage.setTextColor(botTextCol);
        }
    }
}