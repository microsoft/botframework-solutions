import { SET_CABIN_TEMPERATURE } from '../actions/setCabinTemperature';

export default function (state = 21.5, { payload, type }) {
  if (type === SET_CABIN_TEMPERATURE) {
    const { temperature } = payload;

    state = typeof temperature === 'number' ? temperature : false;
  }

  return state;
}
