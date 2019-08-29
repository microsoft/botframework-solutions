import { connect } from 'react-redux';

import Clock from '../Bare/Clock';

export default connect(
  ({ clock }) => ({ value: clock.date })
)(Clock)
