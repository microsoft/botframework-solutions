const SET_CONNECTED = 'SET_CONNECTED';

export default function (connected) {
  return {
    type: SET_CONNECTED,
    payload: { connected }
  };
}

export { SET_CONNECTED }
