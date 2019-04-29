const SET_OUTLOOK_CONNECTIVITY = 'SET_OUTLOOK_CONNECTIVITY';

export default function (connected) {
  return {
    type: SET_OUTLOOK_CONNECTIVITY,
    payload: { connected }
  };
}

export { SET_OUTLOOK_CONNECTIVITY }
