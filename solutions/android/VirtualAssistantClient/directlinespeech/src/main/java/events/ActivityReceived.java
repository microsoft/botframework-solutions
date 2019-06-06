package events;


import client.model.BotConnectorActivity;

public class ActivityReceived {

    public ActivityReceived(BotConnectorActivity botConnectorActivity) {

        this.botConnectorActivity = botConnectorActivity;
    }

    public BotConnectorActivity botConnectorActivity;
}
