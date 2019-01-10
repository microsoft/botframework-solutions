import { SET_OUTLOOK_CONNECTIVITY } from '../actions/setOutlookConnectivity';

export default function (state = false, { payload, type }) {
  if (type === SET_OUTLOOK_CONNECTIVITY) {
    state = !!payload.connected;
  }

  return state;
}
