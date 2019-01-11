const SET_TIMEZONE = 'SET_TIMEZONE';

export default function (name, offset) {
  return {
    type: SET_TIMEZONE,
    payload: { offset, name }
  };
}

export { SET_TIMEZONE }
