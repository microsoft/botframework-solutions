import { OVERRIDE_DIRECT_LINE_SECRET } from '../actions/overrideDirectLineSecret';

export default function (state = null, { payload, type }) {
  if (type === OVERRIDE_DIRECT_LINE_SECRET) {
    state = payload.directLineSecret;
  }

  return state;
}
