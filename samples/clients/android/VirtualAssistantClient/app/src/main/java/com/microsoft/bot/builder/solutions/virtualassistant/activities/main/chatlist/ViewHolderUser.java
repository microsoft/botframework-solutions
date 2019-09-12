package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.support.annotation.NonNull;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.CardView;
import android.support.v7.widget.RecyclerView;
import android.view.View;
import android.widget.TextView;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

import butterknife.BindView;
import butterknife.ButterKnife;

public class ViewHolderUser extends RecyclerView.ViewHolder {

    // CONSTANTS
    private static final String LOGTAG = "ChatViewholder";

    // VIEWS
    @BindView(R.id.tv_chat) TextView textMessage;
    @BindView(R.id.card_user_chat) CardView cardUserChat;

    // STATE
    private View view;

    public ViewHolderUser(@NonNull View itemView) {
        super(itemView);
        view = itemView;
        ButterKnife.bind(this, view);
    }

    /**
     * bind the layout with the data
     */
    void bind(@NonNull ChatModel chatModel, Integer userBubbleCol, Integer userTextCol) {
        textMessage.setText(chatModel.userRequest);
        if (userBubbleCol != null) cardUserChat.setCardBackgroundColor(userBubbleCol);
        if (userTextCol != null) textMessage.setTextColor(userTextCol);
    }
}