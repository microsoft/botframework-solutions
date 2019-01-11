import { connect } from 'react-redux';
import React from 'react';

const SoundTrack = ({
  albumArt,
  className
}) =>
  !!albumArt && <img alt="" className={ className } src={ albumArt } />

export default connect(
  ({ soundTrack }) => ({ albumArt: (soundTrack || {}).albumArt })
)(SoundTrack)
