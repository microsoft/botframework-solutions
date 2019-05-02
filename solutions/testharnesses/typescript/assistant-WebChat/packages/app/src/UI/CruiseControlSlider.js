import { connect } from 'react-redux';
import React from 'react';

import setCruiseControlSpeed from '../redux/actions/setCruiseControlSpeed';
import Slider from './Bare/Slider';

const CruiseControlSlider = ({
  className,
  cruiseControlSpeed,
  setCruiseControlSpeed
}) =>
  <Slider
    className={ className }
    onChange={ setCruiseControlSpeed }
    onChanging={ setCruiseControlSpeed }
    value={ cruiseControlSpeed / 120 }
  />

export default connect(
  ({ cruiseControlSpeed }) => ({ cruiseControlSpeed }),
  dispatch => ({
    setCruiseControlSpeed: fraction => dispatch(setCruiseControlSpeed(fraction * 120))
  })
)(CruiseControlSlider)
