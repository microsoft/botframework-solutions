import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import CruiseControlButton from './CruiseControlButton';
import DashboardButton from './Bare/DashboardButton';

const ROOT_CSS = css({
});

export default ({
  className
}) =>
  <div className={ classNames(
    ROOT_CSS + '',
    (className || '') + ''
  ) }>
    <CruiseControlButton />
    <DashboardButton>
      <i className="ms-Icon ms-Icon--Mail" aria-hidden={ true } />
    </DashboardButton>
    <DashboardButton>
      <i className="ms-Icon ms-Icon--Mail" aria-hidden={ true } />
    </DashboardButton>
    <DashboardButton>
      <i className="ms-Icon ms-Icon--Mail" aria-hidden={ true } />
    </DashboardButton>
  </div>
