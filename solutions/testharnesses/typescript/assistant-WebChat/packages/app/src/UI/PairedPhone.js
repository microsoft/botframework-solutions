import { connect } from 'react-redux';
import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

const ROOT_CSS = css({
});

const PairedPhone = ({
  className,
  pairedPhone
}) =>
  <div
    className={ classNames(
      ROOT_CSS + '',
      (className || '') + ''
    ) }
  >
    {
      pairedPhone ?
        `Connected to ${ pairedPhone }`
      :
        'Not connected'
    }
  </div>

export default connect(
  ({ pairedPhone }) => ({ pairedPhone })
)(PairedPhone)
