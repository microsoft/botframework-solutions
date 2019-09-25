import { connect } from 'react-redux';
import React from 'react';

const NavigationDestinationAddress = ({
  address,
  className
}) => !!address &&
  <div className={ className }>
    { address.split('\n').map((line, index) => <div key={ index }>{ line }</div>) }
  </div>

export default connect(
  ({
    navigationDestination
  }) => ({
    address: (navigationDestination && navigationDestination.address) || ''
  })
)(NavigationDestinationAddress)
