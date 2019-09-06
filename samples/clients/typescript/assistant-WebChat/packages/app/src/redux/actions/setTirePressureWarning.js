const SET_TIRE_PRESSURE_WARNING = 'SET_TIRE_PRESSURE_WARNING';

export default function (warning) {
  return {
    type: SET_TIRE_PRESSURE_WARNING,
    payload: { warning }
  };
}

export { SET_TIRE_PRESSURE_WARNING }
