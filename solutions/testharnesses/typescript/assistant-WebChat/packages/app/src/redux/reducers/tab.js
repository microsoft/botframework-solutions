import { SET_TAB } from '../actions/setTab';

export default function (state = 'Home', { payload, type }) {
  if (type === SET_TAB) {
    state = payload.tab;
  }

  return state;
}
