import { connect } from 'react-redux';
import React from 'react';

import Degree from '../Bare/Degree';

const Longitude = ({
  className,
  value
}) =>
  typeof value === 'number' &&
    <span className={ className }>
      <Degree value={ Math.abs(value) } /> { value >= 0 ? 'N' : 'S' }
    </span>

export default connect(
  ({ geolocation: { longitude: value } }) => ({ value })
)(Longitude)
