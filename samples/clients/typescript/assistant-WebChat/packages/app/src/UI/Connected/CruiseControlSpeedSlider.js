import { connect } from 'react-redux';
import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import setCruiseControlSpeed from '../../redux/actions/setCruiseControlSpeed';
import Slider from '../Bare/Slider';

const ROOT_CSS = css({
  alignItems: 'center',
  display: 'flex',
  width: '100%',

  '> .va__cruisecontrolspeedslider': {
    flex: 1,
    marginRight: 10
  }
});

const CruiseControlSpeedSlider = ({
  className,
  cruiseControlSpeed,
  setCruiseControlSpeed
}) =>
  <div className={ classNames(
    ROOT_CSS + '',
    (className || '') + ''
  ) }>
    <Slider
      className="va__cruisecontrolspeedslider"
      onChange={ setCruiseControlSpeed }
      value={ (cruiseControlSpeed - 40) / 80 }
    />
    <span>{ Math.round(cruiseControlSpeed) }</span>
  </div>

export default connect(
  ({ cruiseControlSpeed }) => ({ cruiseControlSpeed }),
  { setCruiseControlSpeed: value => setCruiseControlSpeed(value * 80 + 40) }
)(CruiseControlSpeedSlider)
