import { SET_EXTERIOR_TEMPERATURE } from '../actions/setExteriorTemperature';

export default function (state = 12, { payload, type }) {
  if (type === SET_EXTERIOR_TEMPERATURE) {
    state = payload.temperature;
  }

  return state;
}
