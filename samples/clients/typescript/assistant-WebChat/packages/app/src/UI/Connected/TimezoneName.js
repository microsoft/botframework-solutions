import { connect } from 'react-redux';
import React from 'react';

const TimezoneName = ({ className, timezoneName }) =>
  !!timezoneName && <span className={ className }>{ timezoneName }</span>

export default connect(
  ({ timezone }) => ({
    timezoneName: (timezone || {}).name
  })
)(TimezoneName)
