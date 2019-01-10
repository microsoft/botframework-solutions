const SET_LAT_LONG = 'SET_LAT_LONG';

export default function (latitude, longitude) {
  return {
    type: SET_LAT_LONG,
    payload: {
      latitude,
      longitude
    }
  };
}

export { SET_LAT_LONG }
