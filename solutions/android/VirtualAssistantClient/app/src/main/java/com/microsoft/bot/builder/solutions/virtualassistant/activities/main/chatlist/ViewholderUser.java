package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.support.annotation.NonNull;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.CardView;
import android.support.v7.widget.RecyclerView;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.TextView;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.utils.LogUtils;
import com.microsoft.bot.builder.solutions.virtualassistant.utils.RawUtils;

import org.json.JSONObject;

import butterknife.BindView;
import butterknife.ButterKnife;
import client.model.BotConnectorActivity;
import io.adaptivecards.objectmodel.AdaptiveCard;
import io.adaptivecards.objectmodel.HostConfig;
import io.adaptivecards.objectmodel.ParseResult;
import io.adaptivecards.renderer.AdaptiveCardRenderer;
import io.adaptivecards.renderer.RenderedAdaptiveCard;

public class ViewholderUser extends RecyclerView.ViewHolder {

    // CONSTANTS
    private static final String LOGTAG = "ChatViewholder";

    // VIEWS
    @BindView(R.id.tv_chat) TextView textMessage;

    // STATE
    private View view;

    public ViewholderUser(@NonNull View itemView) {
        super(itemView);
        view = itemView;
        ButterKnife.bind(this, view);
    }

    /**
     * bind the layout with the data
     */
    void bind(@NonNull ChatModel chatModel, AppCompatActivity parentActivity) {
            textMessage.setText(chatModel.userRequest);
    }
}