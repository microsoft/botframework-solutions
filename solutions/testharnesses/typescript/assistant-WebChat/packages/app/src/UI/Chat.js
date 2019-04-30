import { connect } from 'react-redux';
import { css } from 'glamor';
import classNames from 'classnames';

import
  ReactWebChat,
  {
    createCognitiveServicesSpeechServicesPonyfillFactory,
    createDirectLine
  } from 'botframework-webchat';

import memoizeWithDispose from 'memoize-one-with-dispose';
import React from 'react';

import WebChatStoreContext from '../WebChatStoreContext';

const ROOT_CSS = css({
});

const WEB_CHAT_STYLE_OPTIONS = {
  backgroundColor: 'rgba(255, 255, 255, .7)'
};

class Chat extends React.Component {
  constructor(props) {
    super(props);

    this.memoizedCreateDirectLine = memoizeWithDispose(directLineOptions => {
      if (!directLineOptions) { return; }

      const {
        domain,
        token,
        webSocket
      } = directLineOptions;

      return createDirectLine({
        domain,
        token,
        webSocket
      });
    },
    (x, y) => JSON.stringify(x) === JSON.stringify(y),
    () => {
      // TODO: We should stop DirectLineJS to prevent resources leak
    });

    this.state = { speechServicesPonyfill: null };
  }

  componentWillReceiveProps(nextProps) {
    if (nextProps.speechServicesOptions !== this.props.speechServicesOptions) {
      createCognitiveServicesSpeechServicesPonyfillFactory(nextProps.speechServicesOptions).then(speechServicesPonyfill => {
        this.setState(() => ({ speechServicesPonyfill }));
      });
    }
  }

  componentWillUnmount() {
    this.memoizedCreateDirectLine(null);
  }

  render() {
    const
      {
        props: { className, directLineOptions, languageCode },
        state: { speechServicesPonyfill }
      }
    = this;

    if (!directLineOptions) {
      return false;
    }

    return (
      <WebChatStoreContext.Consumer>
        { store =>
          <ReactWebChat
            className={ classNames(ROOT_CSS + '', className) }
            directLine={ this.memoizedCreateDirectLine(directLineOptions) }
            locale={ languageCode }
            webSpeechPonyfillFactory={ speechServicesPonyfill }
            store={ store }
            styleOptions={ WEB_CHAT_STYLE_OPTIONS }
          />
        }
      </WebChatStoreContext.Consumer>
    );
  }
}

export default connect(
  ({
    directLineOptions,
    geolocation,
    language: { languageCode },
    speechServicesOptions
  }) => ({
    directLineOptions,
    geolocation,
    languageCode,
    speechServicesOptions
  })
)(Chat)
