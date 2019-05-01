import { SET_TIRE_PRESSURE_WARNING } from '../actions/setTirePressureWarning';

export default function (state = false, { payload, type }) {
  if (type === SET_TIRE_PRESSURE_WARNING) {
    state = !!payload.warning;
  }

  return state;
}
