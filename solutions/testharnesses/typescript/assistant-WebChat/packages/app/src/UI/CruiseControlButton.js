import { connect } from 'react-redux';
import React from 'react';

import DashboardButton from './Bare/DashboardButton';
import setCruiseControlSpeed from '../redux/actions/setCruiseControlSpeed';

const CruiseControlButton = ({
  cruiseControl,
  toggleCruiseControl
}) =>
  <DashboardButton
    checked={ cruiseControl }
    onClick={ toggleCruiseControl }
  >
    <i className="ms-Icon ms-Icon--Mail" aria-hidden={ true } />
  </DashboardButton>

export default connect(
  ({ cruiseControl }) => ({ cruiseControl }),
  dispatch => ({
    toggleCruiseControl: () => dispatch(setCruiseControlSpeed(false))
  })
)(CruiseControlButton)
