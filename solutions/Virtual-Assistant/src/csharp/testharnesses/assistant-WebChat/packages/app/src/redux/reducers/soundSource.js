import { SET_SOUND_SOURCE } from '../actions/setSoundSource';

export default function (state = '107.85 FM', { payload, type }) {
  if (type === SET_SOUND_SOURCE) {
    state = payload.source;
  }

  return state;
}
