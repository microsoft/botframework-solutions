const SET_CABIN_TEMPERATURE = 'SET_CABIN_TEMPERATURE';

export default function (temperature) {
  return {
    type: SET_CABIN_TEMPERATURE,
    payload: { temperature }
  };
}

export { SET_CABIN_TEMPERATURE }
