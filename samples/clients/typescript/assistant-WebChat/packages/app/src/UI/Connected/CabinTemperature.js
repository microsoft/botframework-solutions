import { connect } from 'react-redux';
import Temperature from '../Bare/Temperature';

export default connect(
  ({ cabinTemperature, temperatureUnit }) => ({ celsius: cabinTemperature, unit: temperatureUnit })
)(Temperature)
