const SET_TRACTION_CONTROL = 'SET_TRACTION_CONTROL';

export default function (tractionControl) {
  return {
    type: SET_TRACTION_CONTROL,
    payload: tractionControl
  };
}

export { SET_TRACTION_CONTROL }
