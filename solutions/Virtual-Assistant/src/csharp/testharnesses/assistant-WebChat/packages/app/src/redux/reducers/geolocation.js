import updateIn from 'simple-update-in';

import { SET_LAT_LONG } from '../actions/setLatLong';
import { OVERRIDE_LAT_LONG } from '../actions/overrideLatLong';

const DEFAULT_STATE = {
  actualLatitude: undefined,
  actualLongitude: undefined,
  latitude: undefined,
  longitude: undefined,
  overrodeLatitude: undefined,
  overrodeLongitude: undefined
};

export default function (state = DEFAULT_STATE, { payload, type }) {
  if (type === SET_LAT_LONG) {
    const { latitude, longitude } = payload;

    state = updateIn(state, ['actualLatitude'], () => latitude);
    state = updateIn(state, ['actualLongitude'], () => longitude);
  } else if (type === OVERRIDE_LAT_LONG) {
    const { latitude, longitude } = payload;

    state = updateIn(state, ['overrodeLatitude'], () => latitude);
    state = updateIn(state, ['overrodeLongitude'], () => longitude);
  }

  state = updateIn(
    state,
    ['latitude'],
    () => (typeof state.overrodeLatitude === 'number' && !isNaN(state.overrodeLatitude)) ? state.overrodeLatitude : state.actualLatitude
  );

  state = updateIn(
    state,
    ['longitude'],
    () => (typeof state.overrodeLongitude === 'number' && !isNaN(state.overrodeLatitude)) ? state.overrodeLongitude : state.actualLongitude
  );

  return state;
}
