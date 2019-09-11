import { Provider } from 'react-redux';
import random from 'math-random';
import React from 'react';
import ReactDOM from 'react-dom';

import './index.css';
import * as serviceWorker from './serviceWorker';
import App from './App';

import connectUsingSecret from './redux/actions/connectUsingSecret';
import connectUsingTokenServer from './redux/actions/connectUsingTokenServer';
import enableSpeechUsingSecret from './redux/actions/enableSpeechUsingSecret';
import enableSpeechUsingTokenServer from './redux/actions/enableSpeechUsingTokenServer';
import createStore from './redux/createStore';

const store = createStore();

// TODO: Move it somewhere
const { directLineSecret, speechServicesSubscriptionKey } = store.getState();

if (directLineSecret) {
  store.dispatch(connectUsingSecret(directLineSecret, random().toString(36).substr(2, 10)));
} else {
  store.dispatch(connectUsingTokenServer());
}

if (speechServicesSubscriptionKey) {
  store.dispatch(enableSpeechUsingSecret(speechServicesSubscriptionKey));
} else {
  store.dispatch(enableSpeechUsingTokenServer());
}

ReactDOM.render(
  <Provider store={ store }>
    <App />
  </Provider>,
  document.getElementById('root')
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: http://bit.ly/CRA-PWA
serviceWorker.unregister();
