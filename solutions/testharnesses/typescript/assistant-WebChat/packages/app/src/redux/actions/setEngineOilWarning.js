const SET_ENGINE_OIL_WARNING = 'SET_ENGINE_OIL_WARNING';

export default function (warning) {
  return {
    type: SET_ENGINE_OIL_WARNING,
    payload: { warning }
  };
}

export { SET_ENGINE_OIL_WARNING }
