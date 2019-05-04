import { SET_SOUND_TRACK } from '../actions/setSoundTrack';

const DEFAULT_STATE = {
  albumArt: null,
  name: 'DAFT PUNK - Robot Rock'
};

export default function (state = DEFAULT_STATE, { payload, type }) {
  if (type === SET_SOUND_TRACK) {
    const { albumArt, name } = payload;

    state = { albumArt, name };
  }

  return state;
}
