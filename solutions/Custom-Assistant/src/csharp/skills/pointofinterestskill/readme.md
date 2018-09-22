# Point of Interest Bot

# LUIS logic
The Maluuba LUIS model has two intents: NAVIGATION_FROM_X_TO_Y & NAVIGATION_CANCEL_ROUTE, and two entities: KEYWORD & ADDRESS.
NAVIGATION_FROM_X_TO_Y is used for all scenarios.
Find POI/FindRoute: NAVIGATION_FROM_X_TO_Y triggers NavigationDialog and looks up POI cards first before getting directions.
What's Nearby: NAVIGATION_FROM_X_TO_Y triggers NavigationDialog with no keyword or address matched.
Find Along Route: NAVIGATION_FROM_X_TO_Y triggers NavigationDialog and checks for ActiveRoute.

# Event Input
Sample event for setting PointOfInterestBotState.CurrentCoordinates (hardcoded):
User: /event:{ "Name": "IPA.Location", "Value": { "lat": 47.640568390488625, "lon": -122.1293731033802  } }

# Not test
Button validation on cards (current bug on emulator prevents submit.action buttons from being used)
For ActiveLocation selection (after POI is displayed): copy/paste the address to match text instead
For ActiveRoute selection: send any text. Only one route displayed from API in my tests so we're just returning the first in list.'