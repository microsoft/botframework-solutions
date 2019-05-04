import {
  CONNECT_PENDING,
  CONNECT_REJECTED,
  CONNECT_FULFILLED
} from './connect';

export default function (url = '/api/directline/token') {
  return async dispatch => {
    dispatch({ type: CONNECT_PENDING });

    try {
      const res = await fetch(url, { method: 'POST' });

      if (!res.ok) {
        throw new Error(`failed to get Direct Line token, server returned ${ res.status }`);
      }

      const { token } = JSON.parse(await res.text());

      dispatch({ type: CONNECT_FULFILLED, payload: { token } });
    } catch (err) {
      dispatch({ type: CONNECT_REJECTED, error: true, payload: err });
    }
  };
}
