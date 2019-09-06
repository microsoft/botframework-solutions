import { SET_TEMPERATURE_UNIT } from '../actions/setTemperatureUnit';

export default function (state = 'celsius', { payload, type }) {
  if (type === SET_TEMPERATURE_UNIT) {
    state = payload.temperatureUnit === 'fahrenheit' ? 'fahrenheit' : 'celsius';
  }

  return state;
}
