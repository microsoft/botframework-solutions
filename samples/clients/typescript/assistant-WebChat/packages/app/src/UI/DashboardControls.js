import { css } from 'glamor';
import { connect } from 'react-redux';
import classNames from 'classnames';
import React from 'react';

import Home from './Tabs/Home';
import Settings from './Tabs/Settings';
import Test from './Tabs/Test';

import setTab from '../redux/actions/setTab';

import TabBar from './Bare/TabBar';

const ROOT_CSS = css({
  display: 'flex',
  flex: 1,
  flexDirection: 'column',
  justifyContent: 'flex-end',

  '& > .va__content': {
    flexGrow: 10000
  },

  '& > .va__filler': {
    flex: 1,
    flexGrow: 1,
    flexShrink: 10000
  },

  '& > .va__tabbar': {
    flexShrink: 0
  }
});

const ICONS = [
  'Snowflake',
  'Car',
  'MapPin',
  'MusicInCollection',
  'CellPhone',
  'Settings',
  'Home'
];

const DashboardControls = ({
  className,
  setTab,
  tab
}) =>
  <div className={ classNames(
    ROOT_CSS + '',
    (className || '') + ''
  ) }>
    { tab === 'Home' && <Home className="va__content" /> }
    { tab === 'Settings' && <Settings className="va__content" /> }
    { tab === 'TestBeaker' && <Test className="va__content" /> }
    <div className="va__filler" />
    <TabBar
      className="va__tabbar"
      icons={ ICONS }
      onClick={ setTab }
      value={ tab }
    />
  </div>

export default connect(
  ({ tab }) => ({ tab }),
  { setTab }
)(DashboardControls)
