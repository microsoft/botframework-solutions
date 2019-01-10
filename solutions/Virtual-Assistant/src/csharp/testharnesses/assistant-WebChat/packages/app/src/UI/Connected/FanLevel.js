import { connect } from 'react-redux';
import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import setFanLevel from '../../redux/actions/setFanLevel';
import Slider from '../Bare/Slider';

import WebChatStoreContext from '../../WebChatStoreContext';

const ROOT_CSS = css({
  alignItems: 'center',
  display: 'flex',
  width: '100%',

  '> .va__fanlevelslider': {
    flex: 1,
    marginRight: 10
  }
});

const FanLevel = ({
  className,
  fanLevel,
  setFanLevel
}) =>
  <div className={ classNames(
    ROOT_CSS + '',
    (className || '') + ''
  ) }>
    <Slider
      className="va__fanlevelslider"
      onChange={ setFanLevel }
      value={ (fanLevel - 1) / 4 }
    />
    <span>{ fanLevel }</span>
  </div>

const ConnectedFanLevel = connect(
  ({ fanLevel }) => ({ fanLevel }),
  (dispatch, { webChatStore }) => ({
    setFanLevel: value => {
      const fanLevel = Math.round(value * 4 + 1);

      dispatch(setFanLevel(fanLevel));

      webChatStore.dispatch({
        type: 'DIRECT_LINE/POST_ACTIVITY',
        payload: {
          activity: {
            name: 'ChangeFanLevel',
            type: 'event',
            value: fanLevel
          }
        }
      });
    }
  })
)(FanLevel)

export default (...props) =>
  <WebChatStoreContext.Consumer>
    { webChatStore =>
      <ConnectedFanLevel { ...props } webChatStore={ webChatStore } />
    }
  </WebChatStoreContext.Consumer>
