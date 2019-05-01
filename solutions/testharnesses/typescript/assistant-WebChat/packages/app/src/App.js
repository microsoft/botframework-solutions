import { connect } from 'react-redux';
import { css } from 'glamor';
import React from 'react';
import updateIn from 'simple-update-in';

import { createStore } from 'botframework-webchat';

import WebChatStoreContext from './WebChatStoreContext';

import setCabinTemperature from './redux/actions/setCabinTemperature';
import setNavigationDestination from './redux/actions/setNavigationDestination';
import setSoundSource from './redux/actions/setSoundSource';
import setSoundTrack from './redux/actions/setSoundTrack';

import Chat from './UI/Chat';
import DashboardControls from './UI/DashboardControls';

const ROOT_CSS = css({
  display: 'grid',
  gridGap: 10,
  gridTemplateColumns: 'auto minmax(320px, 25%)',
  height: 'calc(100% - 20px)',
  padding: 10,
  width: 'calc(100% - 20px)',

  '& > .va__chat': {
    display: 'flex',
    gridColumn: 1,
    overflowY: 'auto'
  },

  '& > .va__controls': {
    display: 'flex',
    gridColumn: 2,
    overflow: 'hidden'
  }
});

class App extends React.Component {
  constructor(props) {
    super(props);

    this.state = {
      webChatStore: createStore(
        {},
        ({ dispatch }) => next => action => {
          // console.log(action);

          try {
            const { payload, type } = action;

            if (type === 'DIRECT_LINE/INCOMING_ACTIVITY') {
              const { activity } = payload;

              if (
                activity.type === 'event'
                && activity.name === 'ActiveRoute.Directions'
              ) {
                console.log(activity);

                try {
                  this.props.setNavigationDestination(
                    activity.value.Destination.name,
                    activity.value.Destination.address.formattedAddress
                    // estimatedTimeOfArrival: new Date(Date.now() + 15 * 60000),
                  );
                } catch (err) {
                  console.error(err);
                }
              }

              if (
                activity.type === 'event'
                && activity.name === 'ChangeTemperature'
              ) {
                this.props.setCabinTemperature(+activity.value);
              }

              if (
                activity.type === 'event'
                && activity.name === 'TuneRadio'
              ) {
                this.props.setSoundSource(activity.value);
                this.props.setSoundTrack('DAFT PUNK - Robot Rock');
              }

              if (
                activity.type === 'event'
                && activity.name === 'PlayMusic'
              ) {
                this.props.setSoundSource('Bluetooth');
                this.props.setSoundTrack(activity.value);
              }
            } else if (type === 'DIRECT_LINE/CONNECTION_STATUS_UPDATE' && payload.connectionStatus === 2) {
              dispatch({
                type: 'DIRECT_LINE/POST_ACTIVITY',
                payload: {
                  activity: {
                    name: 'startConversation',
                    type: 'event',
                    value: ''
                  }
                }
              });
            } else if (type === 'DIRECT_LINE/POST_ACTIVITY') {
              const { heading, geolocation: { latitude, longitude } } = this.props;

              if (typeof heading === 'number' && !isNaN(heading)) {
                action = updateIn(action, ['channelData', 'heading'], () => heading);
              }

              if (!isNaN(latitude) && !isNaN(longitude)) {
                action = updateIn(action, ['channelData', 'latLong'], () => ({ latitude, longitude }));
              }
            }

            return next(action);
          } catch (err) {
            console.error(err);

            throw err;
          }
        }
      )
    };
  }

  render() {
    const { webChatStore } = this.state;

    return (
      <WebChatStoreContext.Provider value={ webChatStore }>
        <div className={ ROOT_CSS }>
          <Chat className="va__chat" />
          <div className="va__controls">
            <DashboardControls />
          </div>
        </div>
      </WebChatStoreContext.Provider>
    );
  }
}

export default connect(
  ({
    directLineOptions,
    geolocation,
    heading
  }) => ({
    directLineOptions,
    geolocation,
    heading
  }),
  {
    setCabinTemperature,
    setNavigationDestination,
    setSoundSource,
    setSoundTrack
  }
)(App)
