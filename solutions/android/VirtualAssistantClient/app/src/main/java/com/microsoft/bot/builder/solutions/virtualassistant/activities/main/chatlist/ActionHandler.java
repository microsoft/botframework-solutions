package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.util.Log;

import io.adaptivecards.objectmodel.BaseActionElement;
import io.adaptivecards.objectmodel.BaseCardElement;
import io.adaptivecards.renderer.RenderedAdaptiveCard;
import io.adaptivecards.renderer.actionhandler.ICardActionHandler;

public class ActionHandler implements ICardActionHandler {

    public static final String LOGTAG = "ActionHandler";

    @Override
    public void onAction(BaseActionElement baseActionElement, RenderedAdaptiveCard renderedAdaptiveCard) {
        Log.d(LOGTAG, "onAction()");
    }

    @Override
    public void onMediaPlay(BaseCardElement baseCardElement, RenderedAdaptiveCard renderedAdaptiveCard) {
        Log.d(LOGTAG, "onMediaPlay()");
    }

    @Override
    public void onMediaStop(BaseCardElement baseCardElement, RenderedAdaptiveCard renderedAdaptiveCard) {
        Log.d(LOGTAG, "onMediaStop()");
    }
}
