import { connect } from 'react-redux';
import React from 'react';

const NavigationDestinationName = ({
  className,
  name
}) => !!name && <span className={ className }>{ name }</span>

export default connect(
  ({
    navigationDestination
  }) => ({
    name: (navigationDestination && navigationDestination.name) || ''
  })
)(NavigationDestinationName)
