import { connect } from 'react-redux';
import React from 'react';

const SoundSource = ({
  className,
  source
}) =>
  !!source && <span className={ className }>{ source }</span>

export default connect(
  ({ soundSource }) => ({ source: soundSource })
)(SoundSource)
