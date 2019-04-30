import classNames from 'classnames';
import React from 'react';

export default ({ className, icon }) =>
  <i className={ classNames(
    `ms-Icon ms-Icon--${ icon }`,
    className
  ) } />
