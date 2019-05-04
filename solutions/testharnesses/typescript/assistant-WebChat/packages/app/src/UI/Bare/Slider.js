import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import UIFabricIcon from './UIFabricIcon';

const SLIDER_SIZE = 14;

const ROOT_CSS = css({
  alignItems: 'center',
  display: 'flex',
  height: SLIDER_SIZE,
  position: 'relative',
  touchAction: 'none',
  userSelect: 'none',

  '& > .va__handlerbox': {
    cursor: 'pointer',
    display: 'flex',
    position: 'absolute',
    height: '100%',
    width: `calc(100% - ${ SLIDER_SIZE }px)`,

    '& > .va__handler': {
      alignItems: 'center',
      display: 'flex',
      fontSize: SLIDER_SIZE,
      width: SLIDER_SIZE,

      '& > .va__handlerlayer': {
        height: SLIDER_SIZE,
        position: 'relative',

        '& > .ms-Icon': {
          left: 0,
          position: 'absolute',
          top: 0
        },

        '& > .va__fill': {
          color: 'White'
        }
      }
    },

    '& > .va__jumper.va__jumper--right': {
      flex: 1,
      marginRight: -SLIDER_SIZE
    }
  },

  '& > .va__track': {
    borderTop : 'solid 1px #CCC',
    width: '100%'
  }
});

export default class extends React.Component {
  constructor(props) {
    super(props);

    this.handlerBoxRef = React.createRef();

    this.handleLeftJumperClick = this.handleJumperClick.bind(this, value => value - .1);
    this.handleRightJumperClick = this.handleJumperClick.bind(this, value => value + .1);
    this.handlePointerDown = this.handlePointerDown.bind(this);
    this.handlePointerMove = this.handlePointerMove.bind(this);
    this.handlePointerUp = this.handlePointerUp.bind(this);

    this.state = {
      currentPointerID: null,
      intermediateValue: null
    };
  }

  getValue(clientX) {
    const { anchorClientX, anchorValue } = this.state;
    const { current } = this.handlerBoxRef;
    const deltaXFraction = (clientX - anchorClientX) / current.offsetWidth;

    return Math.min(1, Math.max(0, anchorValue + deltaXFraction));
  }

  handleJumperClick(updater) {
    const { value } = this.props;

    this.props.onChange && this.props.onChange(Math.min(1, Math.max(0, updater(value))));
  }

  handlePointerDown({ clientX, pointerId, target }) {
    this.setState(({ currentPointerID }) => {
      if (!currentPointerID) {
        target.setPointerCapture(pointerId);

        return {
          anchorClientX: clientX,
          anchorValue: this.props.value,
          currentPointerID: pointerId,
          intermediateValue: this.props.value
        };
      }
    });
  }

  handlePointerMove({ clientX, pointerId }) {
    if (pointerId === this.state.currentPointerID) {
      const nextValue = this.getValue(clientX);

      !isNaN(nextValue) && this.props.onChanging && this.props.onChanging(nextValue);

      this.setState(() => ({
        intermediateValue: nextValue
      }));
    }
  }

  handlePointerUp({ clientX, pointerId, target }) {
    if (pointerId === this.state.currentPointerID) {
      const nextValue = this.getValue(clientX);

      this.props.onChange && this.props.onChange(nextValue);
    }

    this.setState(({ currentPointerID }) => {
      if (currentPointerID === pointerId) {
        target.releasePointerCapture(pointerId);

        return {
          anchorClientX: null,
          anchorValue: null,
          currentPointerID: null,
          intermediateValue: null
        };
      }
    });
  }

  render() {
    const {
      props: { className, value: committedValue },
      state: { intermediateValue }
    } = this;

    const value = typeof intermediateValue === 'number' ? intermediateValue : committedValue;

    return (
      <div
        className={ classNames(
          ROOT_CSS + '',
          (className || '') + ''
        ) }
      >
        <div
          className="va__handlerbox"
          ref={ this.handlerBoxRef }
        >
          <div
            className="va__jumper"
            onClick={ this.handleLeftJumperClick }
            style={{ width: (value || 0) * 100 + '%' }}
          />
          <div
            className="va__handler"
            onPointerDown={ this.handlePointerDown }
            onPointerMove={ this.handlePointerMove }
            onPointerUp={ this.handlePointerUp }
          >
            <div className="va__handlerlayer">
              <UIFabricIcon className="va__fill" icon="CircleFill" />
              <UIFabricIcon icon="CircleRing" />
            </div>
          </div>
          <div
            className="va__jumper va__jumper--right"
            onClick={ this.handleRightJumperClick }
          />
        </div>
        <div className="va__track" />
      </div>
    );
  }
}
