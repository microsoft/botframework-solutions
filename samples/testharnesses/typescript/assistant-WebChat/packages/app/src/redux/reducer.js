import { combineReducers } from 'redux';

import cabinTemperature from './reducers/cabinTemperature';
import clock from './reducers/clock';
import cruiseControlSpeed from './reducers/cruiseControlSpeed';
import directLineOptions from './reducers/directLineOptions';
import directLineSecret from './reducers/directLineSecret';
import distanceUnit from './reducers/distanceUnit';
import engineOilWarning from './reducers/engineOilWarning';
import exteriorTemperature from './reducers/exteriorTemperature';
import fanLevel from './reducers/fanLevel';
import geolocation from './reducers/geolocation';
import heading from './reducers/heading';
import language from './reducers/language';
import navigationDestination from './reducers/navigationDestination';
import outlookConnected from './reducers/outlookConnected';
import pairedPhone from './reducers/pairedPhone';
import soundSource from './reducers/soundSource';
import soundTrack from './reducers/soundTrack';
import speechServicesOptions from './reducers/speechServicesOptions';
import speechServicesSubscriptionKey from './reducers/speechServicesSubscriptionKey';
import tab from './reducers/tab';
import temperatureUnit from './reducers/temperatureUnit';
import timezone from './reducers/timezone';
import tirePressureWarning from './reducers/tirePressureWarning';
import tractionControl from './reducers/tractionControl';

export default combineReducers({
  cabinTemperature,
  clock,
  cruiseControlSpeed,
  directLineOptions,
  directLineSecret,
  distanceUnit,
  engineOilWarning,
  exteriorTemperature,
  fanLevel,
  geolocation,
  heading,
  language,
  navigationDestination,
  outlookConnected,
  pairedPhone,
  soundSource,
  soundTrack,
  speechServicesOptions,
  speechServicesSubscriptionKey,
  tab,
  temperatureUnit,
  timezone,
  tirePressureWarning,
  tractionControl
})
