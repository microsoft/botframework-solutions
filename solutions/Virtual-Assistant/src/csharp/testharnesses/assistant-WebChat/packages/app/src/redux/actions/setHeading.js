const SET_HEADING = 'SET_HEADING';

export default function (heading) {
  return {
    type: SET_HEADING,
    payload: { heading }
  };
}

export { SET_HEADING }
