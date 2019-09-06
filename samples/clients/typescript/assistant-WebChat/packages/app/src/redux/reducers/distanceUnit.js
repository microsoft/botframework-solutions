import { SET_DISTANCE_UNIT } from '../actions/setDistanceUnit';

export default function (state = 'kilometer', { payload, type }) {
  if (type === SET_DISTANCE_UNIT) {
    state = payload.distanceUnit === 'mile' ? 'mile' : 'kilometer';
  }

  return state;
}
