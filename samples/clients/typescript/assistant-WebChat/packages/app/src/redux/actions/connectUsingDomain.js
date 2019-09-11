import {
  CONNECT_PENDING,
  CONNECT_REJECTED,
  CONNECT_FULFILLED
} from './connect';

export default function (domain, webSocket = false) {
  return dispatch => {
    dispatch({ type: CONNECT_PENDING });
    dispatch({ type: CONNECT_FULFILLED, payload: { domain, webSocket } });
    dispatch({ type: CONNECT_REJECTED, error: true, payload: err });
  };
}
