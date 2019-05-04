import { CONNECT_FULFILLED } from '../actions/connect';
import { DISCONNECT } from '../actions/disconnect';

export default function (state = null, { payload, type }) {
  switch (type) {
    case CONNECT_FULFILLED:
      state = payload;
      break;

    case DISCONNECT:
      state = null;
      break;

    default: break;
  }

  return state;
}
