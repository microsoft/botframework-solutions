import { css } from 'glamor';
import { connect } from 'react-redux';
import React from 'react';

import CabinTemperature from '../Connected/CabinTemperature';
import Clock from '../Connected/Clock';
import Compass from '../Connected/Compass';
import CruiseControlSlider from '../CruiseControlSlider';
import DashboardButtons from '../DashboardButtons';
import Distance from '../Bare/Distance';
import DummySlider from '../Bare/DummySlider';
import ExteriorTemperature from '../Connected/ExteriorTemperature';
import FanLevel from '../Connected/FanLevel';
import Latitude from '../Connected/Latitude';
import Longitude from '../Connected/Longitude';
import MediaControl from '../Bare/MediaControl';
import NavigationDestinationAddress from '../Connected/NavigationDestinationAddress';
import NavigationDestinationName from '../Connected/NavigationDestinationName';
import PairedPhone from '../PairedPhone';
import Speedometer from '../Connected/Speedometer';
import SoundSource from '../Connected/SoundSource';
import SoundTrackName from '../Connected/SoundTrackName';
import SoundTrackAlbumArt from '../Connected/SoundTrackAlbumArt';
import Temperature from '../Bare/Temperature';
import TimezoneName from '../Connected/TimezoneName';

const ROOT_CSS = css({
  display: 'flex',
  flex: 1,
  flexDirection: 'column',
  padding: 10
});

const DashboardControls = () =>
  <div className={ ROOT_CSS }>
    <PairedPhone />
    <CruiseControlSlider />
    <Speedometer />
    <DummySlider />
    <DashboardButtons />
    <Compass />
    <MediaControl />
    <Latitude /> <Longitude />
    <Temperature celsius={ 21.5 } />
    <Temperature celsius={ 21.5 } unit="fahrenheit" />
    <div>
      <Clock />
      &nbsp;
      <TimezoneName />
    </div>
    <CabinTemperature />
    <ExteriorTemperature />
    <FanLevel />
    <Distance kilometer={ 10424 } />
    <Distance kilometer={ 10424 } unit="mile" />
    <SoundSource />
    <SoundTrackName />
    <SoundTrackAlbumArt />
    <div>
      <NavigationDestinationName />
      <br />
      <NavigationDestinationAddress />
    </div>
  </div>

export default connect(
  () => ({})
)(DashboardControls)
