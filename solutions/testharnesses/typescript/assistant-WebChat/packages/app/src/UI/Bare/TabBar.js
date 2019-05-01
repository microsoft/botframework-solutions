import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import ChromelessButton from './ChromelessButton';
import UIFabricIcon from './UIFabricIcon';

const ROOT_CSS = css({
  backgroundColor: '#66C',
  display: 'flex',
  justifyContent: 'space-between',
  padding: 10
});

const TAB_BAR_BUTTON_CSS = css({
  borderRadius: '20%',
  color: 'White',
  fontSize: 20,
  height: 40,
  outline: 0,
  padding: 5,
  width: 40,

  '&.active': {
    backgroundColor: 'rgba(255, 255, 255, .3)'
  }
});

export default ({
  className,
  icons,
  onClick,
  value
}) =>
  <div className={ classNames(
    ROOT_CSS + '',
    (className || '') + ''
  ) }>
    { icons.map(icon =>
      <ChromelessButton
        className={ classNames(
          TAB_BAR_BUTTON_CSS + '',
          { active: icon === value }
        ) }
        key={ icon }
        onClick={ onClick && onClick.bind(null, icon) }
      >
        <UIFabricIcon icon={ icon } />
      </ChromelessButton>
    ) }
  </div>
