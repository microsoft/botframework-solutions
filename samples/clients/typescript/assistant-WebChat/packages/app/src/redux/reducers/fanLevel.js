import { SET_FAN_LEVEL } from '../actions/setFanLevel';

export default function (state = 3, { payload, type }) {
  if (type === SET_FAN_LEVEL) {
    state = Math.max(1, Math.min(5, Math.round(payload.fanLevel))) || '3';
  }

  return state;
}
