import { OVERRIDE_SPEECH_SERVICES_SUBSCRIPTION_KEY } from '../actions/overrideSpeechServicesSubscriptionKey';

export default function (state = null, { payload, type }) {
  if (type === OVERRIDE_SPEECH_SERVICES_SUBSCRIPTION_KEY) {
    state = payload.subscriptionKey;
  }

  return state;
}
