import { connect } from 'react-redux';

import Compass from '../Bare/Compass';

export default connect(
  ({ heading }) => ({ heading })
)(Compass)
