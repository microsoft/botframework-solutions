import { connect } from 'react-redux';

import Meter from '../Bare/Meter';

export default connect(({
  cruiseControlSpeed
}) => ({
  degree: (cruiseControlSpeed / 120 * 180) + 180
}))(Meter)
