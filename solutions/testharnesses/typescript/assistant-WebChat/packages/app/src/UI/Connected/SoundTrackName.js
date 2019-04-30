import { connect } from 'react-redux';
import React from 'react';

const SoundTrack = ({
  className,
  trackName
}) =>
  !!trackName && <span className={ className }>{ trackName }</span>

export default connect(
  ({ soundTrack }) => ({ trackName: (soundTrack || {}).name })
)(SoundTrack)
