import random from 'math-random';

import {
  CONNECT_PENDING,
  CONNECT_REJECTED,
  CONNECT_FULFILLED
} from './connect';

export default function (secret, userID = `dl_${ random().toString(36).substr(2, 10) }`) {
  return async dispatch => {
    dispatch({ type: CONNECT_PENDING });

    try {
      const tokenRes = await fetch('https://directline.botframework.com/v3/directline/tokens/generate', {
        body: JSON.stringify({
          User: { Id: userID }
        }),
        headers: {
          authorization: `Bearer ${ secret }`,
          'content-type': 'application/json'
        },
        method: 'POST'
      });

      if (!tokenRes.ok) {
        throw new Error(`failed to exchange Direct Line secret, server returned ${ tokenRes.status }`);
      }

      const { token } = JSON.parse(await tokenRes.text());

      dispatch({ type: CONNECT_FULFILLED, payload: { token } });
    } catch (err) {
      dispatch({ type: CONNECT_REJECTED, error: true, payload: err });
    }
  };
}
