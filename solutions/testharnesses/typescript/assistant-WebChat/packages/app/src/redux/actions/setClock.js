const SET_CLOCK = 'SET_CLOCK';

export default function (date) {
  return {
    type: SET_CLOCK,
    payload: { date }
  };
}

export { SET_CLOCK }
