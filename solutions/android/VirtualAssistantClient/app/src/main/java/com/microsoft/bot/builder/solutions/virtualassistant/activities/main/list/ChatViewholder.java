package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.list;

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
import com.microsoft.bot.builder.solutions.virtualassistant.R;
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

public class ChatViewholder extends RecyclerView.ViewHolder {

    // CONSTANTS
    private static final String LOGTAG = "ChatViewholder";

    // VIEWS
    @BindView(R.id.tv_bot_chat) TextView textMessage;
    @BindView(R.id.adaptive_card_container) LinearLayout adaptiveCardLayout;

    // STATE
    private View view;
    private ActionHandler cardActionHandler;

    public ChatViewholder(@NonNull View itemView) {
        super(itemView);
        view = itemView;
        ButterKnife.bind(this, view);
        cardActionHandler = new ActionHandler();
    }

    /**
     * bind the layout with the data
     * @param botConnectorActivity data
     */
    void bind(@NonNull BotConnectorActivity botConnectorActivity, AppCompatActivity parentActivity) {
        textMessage.setText(botConnectorActivity.getText());

        if (botConnectorActivity.getAttachmentLayout() != null){
            if (botConnectorActivity.getAttachmentLayout().equals("carousel") || botConnectorActivity.getAttachmentLayout().equals("list")){
                Gson gson = new Gson();

                adaptiveCardLayout.setVisibility(View.VISIBLE);
                adaptiveCardLayout.removeAllViews();

                // generate horizontal or vertical carousel of cards
                for (int x = 0; x < botConnectorActivity.getAttachments().size(); x++) {
                    String cardJson = gson.toJson(botConnectorActivity.getAttachments().get(x));

                    try {
                        JSONObject cardJsonObject = new JSONObject(cardJson);
                        String cardBodyJson = cardJsonObject.getString("content");
                        logLargeString("Received Card: " + cardBodyJson);// this JSON can be used with https://adaptivecards.io/designer/

                        ParseResult parseResult = AdaptiveCard.DeserializeFromString(cardBodyJson, AdaptiveCardRenderer.VERSION);
                        AdaptiveCard adaptiveCard = parseResult.GetAdaptiveCard();
                        HostConfig hostConfig = HostConfig.DeserializeFromString(RawUtils.loadHostConfig(parentActivity));
                        RenderedAdaptiveCard renderedCard = AdaptiveCardRenderer.getInstance().render(parentActivity, parentActivity.getSupportFragmentManager(), adaptiveCard, cardActionHandler, hostConfig);

                        View adaptiveCardRendered = renderedCard.getView();

                        // add the card to its individual container to allow for resizing
                        View adaptiveCardView = LayoutInflater.from(parentActivity).inflate(R.layout.item_adaptive_card, adaptiveCardLayout, false);
                        CardView itemAdaptiveCard = adaptiveCardView.findViewById(R.id.adaptive_card_container);
                        itemAdaptiveCard.addView(adaptiveCardRendered);

                        // add the cards to the existing layout to make them visible
                        adaptiveCardLayout.addView(itemAdaptiveCard);

                        Log.d("ActionHandler", "renderedAdaptiveCard warnings: "+renderedCard.getWarnings().size() + " " + renderedCard.getWarnings().toString());
                    }
                    catch (Exception ex) {
                        Log.e(LOGTAG,"Error in json: " + ex.getMessage());
                    }
                }
            }
        } else {
            adaptiveCardLayout.setVisibility(View.GONE);
        }
    }

    public void logLargeString(String str) {
        if(str.length() > 3000) {
            Log.i(LOGTAG, str.substring(0, 3000));
            logLargeString(str.substring(3000));
        } else {
            Log.i(LOGTAG, str); // continuation
        }
    }
}