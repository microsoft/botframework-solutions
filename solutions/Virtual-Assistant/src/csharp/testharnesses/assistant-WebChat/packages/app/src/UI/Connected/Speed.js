import { connect } from 'react-redux';

import Distance from '../Bare/Distance';

export default connect(({
  cruiseControlSpeed,
  distanceUnit
}) => ({
  kilometer: cruiseControlSpeed,
  unit: distanceUnit,
  unitText: ''
}))(Distance)
