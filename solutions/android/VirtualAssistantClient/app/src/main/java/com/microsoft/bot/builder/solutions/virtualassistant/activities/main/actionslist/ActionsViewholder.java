package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.actionslist;

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
import client.model.CardAction;
import io.adaptivecards.objectmodel.AdaptiveCard;
import io.adaptivecards.objectmodel.HostConfig;
import io.adaptivecards.objectmodel.ParseResult;
import io.adaptivecards.renderer.AdaptiveCardRenderer;
import io.adaptivecards.renderer.RenderedAdaptiveCard;

public class ActionsViewholder extends RecyclerView.ViewHolder {

    // CONSTANTS
    private static final String LOGTAG = "ActionsViewholder";

    // VIEWS
    @BindView(R.id.tv_suggested_action) TextView textMessage;
    @BindView(R.id.action_container) LinearLayout parentLayout;

    // STATE
    private View view;

    public interface OnClickListener {
        void suggestedActionClick(int position);
    }

    public ActionsViewholder(@NonNull View itemView) {
        super(itemView);
        view = itemView;
        ButterKnife.bind(this, view);
    }

    /**
     * bind the layout with the data
     * @param cardAction
     */
    void bind(@NonNull CardAction cardAction, AppCompatActivity parentActivity, @NonNull OnClickListener onClickListener, int position) {
        textMessage.setText((String)cardAction.getValue());

        parentLayout.setOnClickListener(v -> {
            onClickListener.suggestedActionClick(position); // callback to activity
        });
    }
}