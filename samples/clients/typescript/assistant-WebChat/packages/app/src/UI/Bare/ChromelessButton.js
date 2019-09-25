import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

const ROOT_CSS = css({
  backgroundColor: 'Transparent',
  border: 0,

  '&:not(:disabled)': {
    cursor: 'pointer'
  }
});

export default ({
  children,
  className,
  disabled,
  onClick
}) =>
  <button
    className={ classNames(
      ROOT_CSS + '',
      (className || '') + ''
    ) }
    disabled={ disabled }
    onClick={ onClick }
  >
    { children }
  </button>
