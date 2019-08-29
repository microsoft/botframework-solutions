import updateIn from 'simple-update-in';

import { ENABLE_SPEECH_FULFILLED } from '../actions/enableSpeech';

const DEFAULT_STATE = {
  authorizationToken: null,
  region: null
};

export default function (state = DEFAULT_STATE, { payload, type }) {
  if (type === ENABLE_SPEECH_FULFILLED) {
    state = updateIn(state, ['region'], () => payload.region);
    state = updateIn(state, ['authorizationToken'], () => payload.authorizationToken);
  }

  return state;
}
