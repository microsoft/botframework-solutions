const SET_FAN_LEVEL = 'SET_FAN_LEVEL';

export default function (fanLevel) {
  return {
    type: SET_FAN_LEVEL,
    payload: { fanLevel }
  };
}

export { SET_FAN_LEVEL }
