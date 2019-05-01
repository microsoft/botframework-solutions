const SET_CRUISE_CONTROL_SPEED = 'SET_CRUISE_CONTROL_SPEED';

export default function (speed) {
  return {
    type: SET_CRUISE_CONTROL_SPEED,
    payload: { speed }
  };
}

export { SET_CRUISE_CONTROL_SPEED }
