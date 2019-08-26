const PAIR_PHONE = 'PAIR_PHONE';

export default function (phoneName) {
  return {
    type: PAIR_PHONE,
    payload: {
      phoneName
    }
  };
}

export { PAIR_PHONE }
