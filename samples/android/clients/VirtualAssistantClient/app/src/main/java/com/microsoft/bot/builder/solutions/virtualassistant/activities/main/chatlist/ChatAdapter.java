package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.arch.lifecycle.ViewModelProviders;
import android.content.Context;
import android.support.annotation.NonNull;
import android.support.v4.app.FragmentActivity;
import android.support.v7.widget.RecyclerView;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.LinearLayout;
import android.widget.RelativeLayout;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.utils.RawUtils;

import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Objects;
import java.util.stream.Collectors;

import client.model.BotConnectorActivity;
import io.adaptivecards.objectmodel.AdaptiveCard;
import io.adaptivecards.objectmodel.HostConfig;
import io.adaptivecards.objectmodel.ParseContext;
import io.adaptivecards.objectmodel.ParseResult;
import io.adaptivecards.renderer.AdaptiveCardRenderer;
import io.adaptivecards.renderer.RenderedAdaptiveCard;
import io.adaptivecards.renderer.actionhandler.ICardActionHandler;
import io.adaptivecards.renderer.registration.CardRendererRegistration;

public class ChatAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder> {

    // CONSTANTS
    private static final String LOGTAG = "ChatAdapter";
    private static final int MSG_TYPE_BOT = -1;
    private static final int MSG_TYPE_USER = -2;

    // STATE
    private Context context;
    private Gson gson;
    private ArrayList<ChatModel> chatList; // visible chat history
    private ArrayList<ChatModel> chatHistory; // full chat history
    private static int MAX_CHAT_ITEMS = 2;
    private boolean showFullConversation;
    private Integer colorBubbleBot;
    private Integer colorBubbleUser;
    private Integer colorTextBot;
    private Integer colorTextUser;
    private ChatViewModel chatViewModel;
    private HostConfig hostConfig;

    public ChatAdapter(Context context) {
        this.context = context;
        this.gson = new GsonBuilder().disableHtmlEscaping().create();

        // load chat history from view model
        chatViewModel = ViewModelProviders.of((FragmentActivity) context).get(ChatViewModel.class);
        chatHistory = chatViewModel.getChatHistory().getValue();
        showFullConversation = chatViewModel.getShowFullConversation().getValue();

        // adaptive cards renderer related
        CardRendererRegistration.getInstance().registerResourceResolver("data", new SvgImageLoader());
        hostConfig = HostConfig.DeserializeFromString(RawUtils.loadHostConfig(context));

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
        RecyclerView.ViewHolder viewHolder;

        if (viewType == MSG_TYPE_BOT) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_chat_bot, parent, false);
            viewHolder = new ViewHolderBot(view);
        } else if (viewType == MSG_TYPE_USER) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_chat_user, parent, false);
            viewHolder = new ViewHolderUser(view);
        } else {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_chat_bot, parent, false);
            viewHolder = new ViewHolderBot(view);

            // render cards when creating view holders for better performance
            ChatModel chatModel = chatList.get(viewType); // use viewType to pass value of position
            BotConnectorActivity botConnectorActivity = chatModel.botConnectorActivity;
            View botCards = view.findViewById(R.id.bot_cards);
            botCards.setVisibility(View.VISIBLE); // bot cards is hidden by default, set to visible if cards exist

            RelativeLayout cardsContainer = view.findViewById(R.id.cards_container);

            // generate horizontal or vertical carousel of cards
            for (int x = 0; x < botConnectorActivity.getAttachments().size(); x++) {
                String cardJson = gson.toJson(botConnectorActivity.getAttachments().get(x));
                try {
                    JSONObject cardJsonObject = new JSONObject(cardJson);
                    String contentType = cardJsonObject.getString("contentType");

                    // only adaptive cards supported for now
                    if (Objects.equals(contentType, "application/vnd.microsoft.card.adaptive")) {
                        JSONObject content = cardJsonObject.getJSONObject("content");
                        ParseContext parseContext = new ParseContext();
                        ParseResult parseResult = AdaptiveCard.DeserializeFromString(content.toString(), AdaptiveCardRenderer.VERSION, parseContext);
                        AdaptiveCard adaptiveCard = parseResult.GetAdaptiveCard();
                        RenderedAdaptiveCard renderedAdaptiveCard = AdaptiveCardRenderer.getInstance().render(
                                context, ((FragmentActivity)context).getSupportFragmentManager(), adaptiveCard, (ICardActionHandler) context, hostConfig);

                        // get view from rendered adaptive card
                        View renderedAdaptiveCardView = renderedAdaptiveCard.getView();
                        renderedAdaptiveCardView.setFocusable(false);
                        renderedAdaptiveCardView.setFocusableInTouchMode(false);
                        renderedAdaptiveCardView.setBackgroundColor(colorBubbleBot);

                        // workaround of adaptive card render issue
                        renderedAdaptiveCardView.setLayoutParams(new LinearLayout.LayoutParams(parent.getWidth() * 3 / 4, LinearLayout.LayoutParams.WRAP_CONTENT));

                        // add the view to the existing card container
                        cardsContainer.addView(renderedAdaptiveCardView);
                    }
                } catch (Exception e) {
                    Log.e(LOGTAG, e.getMessage());
                }
            }
        }

        return viewHolder;
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder viewHolder, int position) {
        ChatModel chatModel = chatList.get(position);
        if (getItemViewType(position) == MSG_TYPE_USER) {
            ((ViewHolderUser)viewHolder).bind(chatModel, colorBubbleUser, colorTextUser);
        } else {
            ((ViewHolderBot)viewHolder).bind(chatModel, colorBubbleBot, colorTextBot);
        }
    }

    @Override
    public int getItemViewType(int position) {
        ChatModel chatModel = chatList.get(position);
        if (!chatModel.isBotMessage()) {
            return MSG_TYPE_USER; // user message
        } else if (!chatModel.hasAttachments()) {
            return MSG_TYPE_BOT; // bot message without cards
        } else {
            return position; // bot message with cards, use viewType to pass value of position
        }
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

    public void addBotResponse(BotConnectorActivity botConnectorActivity) {
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
