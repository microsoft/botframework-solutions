import { SET_HEADING } from '../actions/setHeading';

export default function (state = null, { payload, type }) {
  if (type === SET_HEADING) {
    state = payload.heading;
  }

  return state;
}
