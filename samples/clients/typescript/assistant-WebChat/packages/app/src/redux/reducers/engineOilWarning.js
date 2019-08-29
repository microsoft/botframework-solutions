import { SET_ENGINE_OIL_WARNING } from '../actions/setEngineOilWarning';

export default function (state = false, { payload, type }) {
  if (type === SET_ENGINE_OIL_WARNING) {
    state = !!payload.warning;
  }

  return state;
}
