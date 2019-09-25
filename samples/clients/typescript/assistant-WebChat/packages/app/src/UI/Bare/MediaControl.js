import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import RoundButton from './RoundButton';
import Slider from './Slider'
import UIFabricIcon from './UIFabricIcon';

const ROOT_CSS = css({
  alignItems: 'center',
  display: 'flex',

  '& > .va__progress': {
    flex: 1,
    height: 20,
    margin: 10
  }
});

export default ({
  className
}) =>
  <div className={ classNames(
    ROOT_CSS + '',
    (className || '') + ''
  ) }>
    <RoundButton>
      <UIFabricIcon icon="Previous" />
    </RoundButton>
    <RoundButton>
      <UIFabricIcon icon="Play" />
    </RoundButton>
    <RoundButton>
      <UIFabricIcon icon="Next" />
    </RoundButton>
    <Slider className="va__progress" />
    <RoundButton>
      <UIFabricIcon icon="Volume2" />
    </RoundButton>
  </div>
