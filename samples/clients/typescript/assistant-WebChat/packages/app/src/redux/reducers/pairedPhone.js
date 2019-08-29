import PAIR_PHONE from '../actions/pairPhone';

export default function (state = false, { payload, type }) {
  if (type === PAIR_PHONE) {
    state = payload.phoneName;
  }

  return state;
}
