import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import ChromelessButton from './ChromelessButton';

const ROOT_CSS = css({
  borderRadius: '50%',
  fontSize: 16,
  height: 30,
  lineHeight: '29px',
  outline: 0,
  width: 30,

  '&:hover': {
    backgroundColor: 'rgba(128, 128, 128, .1)'
  }
});

export default ({
  children,
  className,
  disabled,
  onClick
}) =>
  <ChromelessButton
    className={ classNames(
      ROOT_CSS + '',
      (className || '') + ''
    ) }
    disabled={ disabled }
    onClick={ onClick }
  >
    { children }
  </ChromelessButton>
