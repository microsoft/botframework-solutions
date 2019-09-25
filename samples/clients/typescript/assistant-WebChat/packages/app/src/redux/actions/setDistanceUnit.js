const SET_DISTANCE_UNIT = 'SET_DISTANCE_UNIT';

export default function (distanceUnit) {
  return {
    type: SET_DISTANCE_UNIT,
    payload: {
      distanceUnit: distanceUnit === 'mile' ? 'mile' : 'kilometer'
    }
  };
}

export { SET_DISTANCE_UNIT }
