import { SET_TRACTION_CONTROL } from '../actions/setTractionControl';

export default function (state = true, { payload, type }) {
  if (type === SET_TRACTION_CONTROL) {
    state = !!payload.tractionControl;
  }

  return state;
}
