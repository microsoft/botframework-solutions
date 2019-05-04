const SET_EXTERIOR_TEMPERATURE = 'SET_EXTERIOR_TEMPERATURE';

export default function (temperature) {
  return {
    type: SET_EXTERIOR_TEMPERATURE,
    payload: { temperature }
  };
}

export { SET_EXTERIOR_TEMPERATURE }
