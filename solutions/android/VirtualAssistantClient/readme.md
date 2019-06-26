# Virtual Assistant Android Client Application

## Building the Project
### Step 1 - Credentials
1. Edit "DefaultConfiguration.java"
(NOTE there are two: one for debug and one for release build flavors) 

2. Provide the following information:
COGNITIVE_SERVICES_SUBSCRIPTION_KEY
BOT_ID
USER_ID

### Step 2 - Deploy
1. Select the desired build flavor (debug or release)
2. Deploy to emulator or device

## Using the Project
### Running for First Time
- Accept the Record Audio permission to make voice requests from the bot. Without this permission, the user can only type requests to the Bot.
- Accept the Fine Location permission to easily make voice requests that are related to your GPS location, such as "find the nearest coffee shop". Without this permission, the user will be asked by the Bot to provide the current location.
- Slide away the service notification

### The UI
1. The Mic button is the only graphic immediately visible - press it to make a voice request. (The app will sense when you've finished speaking)

2. The Navigation Drawer (on the left side of the screen) provides the following functionality:
- Bot Configuration
- App Configuration
- Reset Bot
- Send Location Event
- Send Welcome Event
- Inject Adaptive Card Event
- Show Assistant Settings
- Show Textinput

### Functionality Overview
#### Bot Configuration
The data on this screen is originally read from "DefaultConfiguration.java". Changing the values on this screen will update the stored data and used immediately.
NOTE: the stored data persists between app installs

#### App Configuration
Settings that are specific to the app can be set here

#### Reset Bot
If the bot enters a problematic state, you can reset it.

#### Send Location Event
Send a location event using the latitude and longitude values found in the "DefaultConfiguration.java"

#### Send Welcome Event
Triggers the Bot to respond with a default "Welcome" card

#### Inject Adaptive Card Event
To test rendering of an Adaptive Card. First, create the adaptive card Json at [Adaptive Cards Designer](https://adaptivecards.io/designer/ "Adaptive Cards Designer").
Second, copy the generated Json into MainActivity.onNavigationItemSelected().
Finally run the app and "inject" the adaptive card to see what it looks like when sent by the Bot.

#### Show Assistant Settings
Shortcut to the Android Assistant Settings

#### Show Textinput
Shows the text input field to make requests without needing to speak



