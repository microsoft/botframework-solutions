import { CLEAR_NAVIGATION_DESTINATION } from '../actions/clearNavigationDestination';
import { SET_NAVIGATION_DESTINATION } from '../actions/setNavigationDestination';

const DEFAULT_STATE = {
  address: '17801 International Boulevard\nSeattle, Washington',
  name: 'SEATAC AIRPORT'
};

export default function (state = DEFAULT_STATE, { payload, type }) {
  if (type === CLEAR_NAVIGATION_DESTINATION) {
    state = null;
  } else if (type === SET_NAVIGATION_DESTINATION) {
    const { address, name } = payload;

    state = { address, name };
  }

  return state;
}
