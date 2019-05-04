import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import UIFabricIcon from './UIFabricIcon';

const ROOT_CSS = css({
  alignItems: 'center',
  display: 'flex',
  height: 20,
  justifyContent: 'center',
  transformOrigin: 'center',
  width: 20
});

export default ({
  className,
  heading
}) =>
  <div
    className={ classNames(
      ROOT_CSS + '',
      (className || '') + ''
    ) }
    style={{ transform: `rotate(${ heading }deg)` }}
  >
    <UIFabricIcon icon="WindDirection" />
  </div>
