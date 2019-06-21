package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.actionslist;

import android.support.annotation.NonNull;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.RecyclerView;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

import java.util.ArrayList;
import java.util.List;

import client.model.CardAction;

public class ActionsAdapter extends RecyclerView.Adapter<ActionsViewholder> {

    // CONSTANTS
    private final int CONTENT_VIEW = R.layout.item_suggested_action;
    private static final String LOGTAG = "ActionsAdapter";

    // STATE
    private ArrayList<CardAction> actionsList = new ArrayList<>();
    private AppCompatActivity parentActivity;
    private ActionsViewholder.OnClickListener clickListener;


    @NonNull
    @Override
    public ActionsViewholder onCreateViewHolder(@NonNull ViewGroup parent, int i) {
        View view = LayoutInflater.from(parent.getContext()).inflate(CONTENT_VIEW, parent, false);
        return new ActionsViewholder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull ActionsViewholder actionsViewholder, int position) {
        CardAction cardAction = actionsList.get(position);
        actionsViewholder.bind(cardAction, parentActivity, clickListener, position);
    }

    @Override
    public int getItemCount() {
        if (actionsList == null) return 0;
        return actionsList.size();
    }

    public void addAll(List<CardAction> list, AppCompatActivity parentActivity, ActionsViewholder.OnClickListener clickListener) {
        this.parentActivity = parentActivity;
        this.clickListener = clickListener;
        actionsList.clear();
        actionsList.addAll(list);
        notifyDataSetChanged();
    }

    public void clear(){
        actionsList.clear();
        notifyDataSetChanged();
    }
}
