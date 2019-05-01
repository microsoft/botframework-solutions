import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import RoundButton from './RoundButton';

const ROOT_CSS = css({
  color: 'Red',

  '&.va--checked': {
    color: 'Green'
  }
});

export default ({
  checked,
  children,
  className,
  disabled,
  onClick
}) =>
  <RoundButton
    className={ classNames(
      ROOT_CSS + '',
      { 'va--checked': checked },
      (className || '') + ''
    ) }
    disabled={ disabled }
    onClick={ onClick }
  >
    { children }
  </RoundButton>
