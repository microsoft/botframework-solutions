import { SET_CRUISE_CONTROL_SPEED } from '../actions/setCruiseControlSpeed';

export default function (state = 74, { payload, type }) {
  if (type === SET_CRUISE_CONTROL_SPEED) {
    const { speed } = payload;

    if (typeof speed === 'number') {
      state = speed;
    } else {
      state = false;
    }
  }

  return state;
}
