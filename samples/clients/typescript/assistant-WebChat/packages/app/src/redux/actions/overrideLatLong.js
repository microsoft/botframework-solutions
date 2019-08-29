const OVERRIDE_LAT_LONG = 'OVERRIDE_LAT_LONG';

export default function (latitude, longitude) {
  return {
    type: OVERRIDE_LAT_LONG,
    payload: {
      latitude,
      longitude
    }
  };
}

export { OVERRIDE_LAT_LONG }
