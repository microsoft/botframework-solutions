import { css } from 'glamor';
// import { connect } from 'react-redux';
import classNames from 'classnames';
import React from 'react';

import CabinTemperature from '../Connected/CabinTemperature';
import Clock from '../Connected/Clock';
import Compass from '../Connected/Compass';
import CruiseControlSpeedSlider from '../Connected/CruiseControlSpeedSlider';
import ExteriorTemperature from '../Connected/ExteriorTemperature';
import FanLevel from '../Connected/FanLevel';
import Latitude from '../Connected/Latitude';
import Longitude from '../Connected/Longitude';
import MediaControl from '../Bare/MediaControl';
import NavigationDestinationAddress from '../Connected/NavigationDestinationAddress';
import NavigationDestinationName from '../Connected/NavigationDestinationName';
import PairedPhone from '../PairedPhone';
import SoundSource from '../Connected/SoundSource';
import SoundTrackAlbumArt from '../Connected/SoundTrackAlbumArt';
import SoundTrackName from '../Connected/SoundTrackName';
import Speed from '../Connected/Speed';
import Speedometer from '../Connected/Speedometer';
import TimezoneName from '../Connected/TimezoneName';
import UIFabricIcon from '../Bare/UIFabricIcon';

const ROOT_CSS = css({
  background: '#3D4449',
  display: 'flex',
  flex: 1,
  flexDirection: 'column',
  userSelect: 'none',

  '& > .va__row': {
    alignItems: 'center',
    display: 'flex',
    minHeight: 20,
    padding: 10
  },

  '& > .va__profilerow': {
    backgroundColor: 'Black',
    color: 'White'
  },

  '& > .va__temperaturerow': {
    color: 'White',
    justifyContent: 'space-around',

    '& > .va__temperaturecolumn': {
      alignItems: 'center',
      display: 'flex',
      flexDirection: 'column',

      '& > .va__temperature': {
        fontSize: 30
      }
    }
  },

  '& > .va__fanrow': {
    backgroundColor: '#2C5472',
    color: 'White'
  },

  '& > .va__speedrow': {
    backgroundColor: 'rgba(255, 255, 255, .5)',
    fontWeight: 200,
    justifyContent: 'stretch',

    '& > .va__speed': {
      color: '#132331',
      flex: 1,
      fontSize: 72,
      textAlign: 'center'
    },

    '& > .va__speedometer': {
      height: 75,
      width: 150
    }
  },

  '& > .va__cruisecontrolrow': {
    alignItems: 'center',
    backgroundColor: '#525B5F',
    color: 'White',

    '& > *:not(:last-child)': {
      marginRight: '.5em'
    },

    '& > .va__cruisecontrolspeedslider': {
      flex: 1
    }
  },

  '& > .va__clockrow': {
    alignItems: 'flex-end',
    backgroundColor: '#525B5F',
    color: 'White',
    justifyContent: 'center',

    '& > .va__clock': {
      fontSize: 24,
      fontWeight: 200,
      lineHeight: '24px',
      marginRight: '.5em'
    },

    '& > .va__timezonename': {
      color: 'rgb(0, 174, 239)'
    }
  },

  '& > .va__navigationrow': {
    alignItems: 'center',
    color: 'White',
    display: 'flex',
    flex: 1,
    flexDirection: 'column',
    padding: '10px 0',

    '& > .va__navigationdestination': {
      display: 'flex',
      flex: 1,
      flexDirection: 'column',
      justifyContent: 'center',
      textAlign: 'center'
    },

    '& > .va__latlong': {
      alignItems: 'stretch',
      alignSelf: 'stretch',
      display: 'flex',
      height: 40,
      justifyContent: 'space-between',
      paddingTop: 10,

      '& > .va__latitude, & > .va__longitude': {
        backgroundColor: 'Black',
        flex: 1,
        fontSize: 12,
        padding: 10
      },

      '& > .va__longitude': {
        textAlign: 'right'
      },

      '& > .va__compass': {
        fontSize: 24,
        marginTop: 10,
        textAlign: 'center',
        width: 50
      }
    }
  },

  '& > .va__radiorow': {
    alignItems: 'stretch',
    color: 'White',
    display: 'flex',
    flexDirection: 'column',
    padding: 0,

    '& > .va__soundinfo': {
      backgroundColor: 'Black',
      display: 'grid',
      gridTemplateColumns: '1fr 3fr',
      padding: 10,

      '& > .va__soundtrackalbumart': {
        gridColumn: 1,
        gridRow: '1 / 2'
      },

      '& > .va__soundtrackname': {
        gridColumn: 2,
        gridRow: 1
      },

      '& > .va__soundsource': {
        color: '#99C',
        gridColumn: 2,
        gridRow: 2
      }
    },

    '& > .va__mediacontrol': {
      backgroundColor: 'rgba(0, 0, 0, .6)',
      padding: '0 10px',

      '& button': {
        color: 'White'
      }
    }
  }
});

export default ({ className }) =>
  <div className={ classNames(
    ROOT_CSS + '',
    (className || '') + ''
  ) }>
    <div className="va__row va__profilerow">
      <UIFabricIcon icon="Contact" />
      &nbsp;
      <PairedPhone />
    </div>
    <div className="va__row va__temperaturerow">
      <div className="va__temperaturecolumn">
        <CabinTemperature className="va__temperature" />
        <div>Cabin Temp</div>
      </div>
      <div className="va__temperaturecolumn">
        <ExteriorTemperature className="va__temperature" />
        <div>Exterior Temp</div>
      </div>
    </div>
    <div className="va__row va__fanrow">
      <FanLevel />
    </div>
    <div className="va__row va__speedrow">
      <Speed className="va__speed" />
      <Speedometer className="va__speedometer" />
    </div>
    <div className="va__row va__cruisecontrolrow">
      <UIFabricIcon icon="SpeedHigh" />
      <span>Cruise Control</span>
      <CruiseControlSpeedSlider className="va__cruisecontrolspeedslider" />
    </div>
    <div className="va__row va__clockrow">
      <Clock className="va__clock" />
      <TimezoneName className="va__timezonename" />
    </div>
    <div className="va__row va__navigationrow">
      <div className="va__navigationdestination">
        <div>Destination:</div>
        <NavigationDestinationName className="va__navigationdestinationname" />
        <NavigationDestinationAddress />
      </div>
      <div className="va__latlong">
        <span className="va__latitude">LAT <nobr><Latitude /></nobr></span>
        <Compass className="va__compass" />
        <span className="va__longitude">LONG <nobr><Longitude /></nobr></span>
      </div>
    </div>
    <div className="va__row va__radiorow">
      <div className="va__soundinfo">
        <SoundTrackAlbumArt className="va__soundtrackalbumart" />
        <SoundTrackName className="va__soundtrackname" />
        <SoundSource className="va__soundsource" />
      </div>
      <MediaControl className="va__mediacontrol" />
    </div>
  </div>
