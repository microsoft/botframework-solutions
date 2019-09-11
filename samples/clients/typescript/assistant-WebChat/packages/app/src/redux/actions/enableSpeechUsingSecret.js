import {
  ENABLE_SPEECH_PENDING,
  ENABLE_SPEECH_REJECTED,
  ENABLE_SPEECH_FULFILLED
} from './enableSpeech';

export default function (subscriptionKey, region = 'westus') {
  return async dispatch => {
    dispatch({ type: ENABLE_SPEECH_PENDING });

    try {
      const tokenRes = await fetch(`https://${ region }.api.cognitive.microsoft.com/sts/v1.0/issueToken`, {
        headers: {
          'Ocp-Apim-Subscription-Key': subscriptionKey
        },
        method: 'POST'
      });

      if (!tokenRes.ok) {
        throw new Error(`failed to exchange Speech Services secret, server returned ${ tokenRes.status }`);
      }

      const authorizationToken = await tokenRes.text();

      dispatch({ type: ENABLE_SPEECH_FULFILLED, payload: { authorizationToken, region: region } });
    } catch (err) {
      dispatch({ type: ENABLE_SPEECH_REJECTED, error: true, payload: err });
    }
  };
}
