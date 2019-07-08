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

public class ViewholderBot extends RecyclerView.ViewHolder {

    // CONSTANTS
    private static final String LOGTAG = "ChatViewholder";

    // VIEWS
    @BindView(R.id.tv_chat) TextView textMessage;
    @BindView(R.id.adaptive_card_container) LinearLayout adaptiveCardLayout;

    // STATE
    private View view;
    private ActionHandler cardActionHandler;

    public interface OnClickListener {
        void adaptiveCardClick(int position, String speak);
    }

    public ViewholderBot(@NonNull View itemView) {
        super(itemView);
        view = itemView;
        ButterKnife.bind(this, view);
        cardActionHandler = new ActionHandler();
    }

    /**
     * bind the layout with the data
     * @param ChatModel chatModel
     */
    void bind(@NonNull ChatModel chatModel, AppCompatActivity parentActivity, @NonNull OnClickListener onClickListener) {
        BotConnectorActivity botConnectorActivity = chatModel.botConnectorActivity;
        textMessage.setText(botConnectorActivity.getText());

        if (botConnectorActivity.getAttachmentLayout() != null && botConnectorActivity.getAttachments().size() > 0) {
            if (botConnectorActivity.getAttachmentLayout().equals("carousel") || botConnectorActivity.getAttachmentLayout().equals("list")) {
                Gson gson = new GsonBuilder().disableHtmlEscaping().create();

                adaptiveCardLayout.setVisibility(View.VISIBLE);
                adaptiveCardLayout.removeAllViews();

                // generate horizontal or vertical carousel of cards
                for (int x = 0; x < botConnectorActivity.getAttachments().size(); x++) {
                    String cardJson = gson.toJson(botConnectorActivity.getAttachments().get(x));

                    try {
                        JSONObject cardJsonObject = new JSONObject(cardJson);
                        JSONObject cardContent = cardJsonObject.getJSONObject("content");
                        String cardBodyJson = cardContent.toString();
                        LogUtils.logLongInfoMessage(LOGTAG, "Received Card: " + cardBodyJson);// this JSON can be used with https://adaptivecards.io/designer/

                        // collect what the payload is when clicked
                        final String speak = cardContent.getString("speak");
                        final int position = x;

                        ParseResult parseResult = AdaptiveCard.DeserializeFromString(cardBodyJson, AdaptiveCardRenderer.VERSION);
                        AdaptiveCard adaptiveCard = parseResult.GetAdaptiveCard();
                        HostConfig hostConfig = HostConfig.DeserializeFromString(RawUtils.loadHostConfig(parentActivity));
                        RenderedAdaptiveCard renderedCard = AdaptiveCardRenderer.getInstance().render(parentActivity, parentActivity.getSupportFragmentManager(), adaptiveCard, cardActionHandler, hostConfig);

                        View adaptiveCardRendered = renderedCard.getView();
                        adaptiveCardRendered.setFocusable(false);
                        adaptiveCardRendered.setFocusableInTouchMode(false);
                        adaptiveCardRendered.setOnClickListener(v -> {
                            onClickListener.adaptiveCardClick(position, speak); // callback to activity
                        });

                        // add the card to its individual container to allow for resizing
                        View adaptiveCardView = LayoutInflater.from(parentActivity).inflate(R.layout.item_adaptive_card, adaptiveCardLayout, false);
                        CardView itemAdaptiveCard = adaptiveCardView.findViewById(R.id.adaptive_card_container);
                        itemAdaptiveCard.addView(adaptiveCardRendered);

                        // add the cards to the existing layout to make them visible
                        adaptiveCardLayout.addView(itemAdaptiveCard);

                        Log.d(LOGTAG, "renderedAdaptiveCard warnings: " + renderedCard.getWarnings().size() + " " + renderedCard.getWarnings().toString());
                    } catch (Exception ex) {
                        Log.e(LOGTAG, "Error in json: " + ex.getMessage());
                    }
                }
            }
        } else {
            adaptiveCardLayout.setVisibility(View.GONE);
        }
    }
}