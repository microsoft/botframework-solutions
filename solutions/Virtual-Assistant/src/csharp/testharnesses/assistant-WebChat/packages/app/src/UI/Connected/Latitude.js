import { connect } from 'react-redux';
import React from 'react';

import Degree from '../Bare/Degree';

const Latitude = ({
  className,
  value
}) =>
  typeof value === 'number' &&
    <span className={ className }>
      <Degree value={ value } /> { value >= 0 ? 'N' : 'S' }
    </span>

export default connect(
  ({ geolocation: { latitude: value } }) => ({ value })
)(Latitude)
