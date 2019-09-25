import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

const ROOT_CSS = css({
  height: 25,
  position: 'relative',
  width: 50,

  '& > .va__hand': {
    borderTop: 'solid 2px rgb(0, 174, 239)',
    bottom: 0,
    boxSizing: 'border-box',
    left: '50%',
    position: 'absolute',
    transformOrigin: '0 0',
    width: '50%'
  }
});

export default ({
  className,
  degree = 0
}) =>
  <div
    className={ classNames(
      ROOT_CSS + '',
      (className || '') + ''
    ) }
  >
    <div
      className="va__hand"
      style={{ transform: `rotate(${ degree }deg)` }}
    />
  </div>
