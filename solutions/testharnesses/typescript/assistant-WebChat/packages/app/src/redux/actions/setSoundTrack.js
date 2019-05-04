const SET_SOUND_TRACK = 'SET_SOUND_TRACK';

export default function (name, albumArt) {
  return {
    type: SET_SOUND_TRACK,
    payload: { albumArt, name }
  };
}

export { SET_SOUND_TRACK }
