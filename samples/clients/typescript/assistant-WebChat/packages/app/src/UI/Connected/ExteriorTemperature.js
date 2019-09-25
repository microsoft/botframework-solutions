import { connect } from 'react-redux';
import Temperature from '../Bare/Temperature';

export default connect(
  ({ exteriorTemperature, temperatureUnit }) => ({ celsius: exteriorTemperature, unit: temperatureUnit })
)(Temperature)
