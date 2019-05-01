const SET_SOUND_SOURCE = 'SET_SOUND_SOURCE';

export default function (source) {
  return {
    type: SET_SOUND_SOURCE,
    payload: {
      source
    }
  };
}

export { SET_SOUND_SOURCE }
