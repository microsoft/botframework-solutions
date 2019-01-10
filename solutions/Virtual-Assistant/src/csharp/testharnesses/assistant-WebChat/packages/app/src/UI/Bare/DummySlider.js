import { css } from 'glamor';
import classNames from 'classnames';
import React from 'react';

import Slider from './Slider';

const ROOT_CSS = css({
  alignItems: 'center',
  display: 'flex',

  '& > .va__slider': {
    flex: 1
  },

  '& > .va__value': {
    overflow: 'hidden',
    paddingLeft: 10,
    textAlign: 'right',
    width: '2.5em'
  }
});

export default class extends React.Component {
  constructor(props) {
    super(props);

    this.handleChange = this.handleChange.bind(this);
    this.handleChanging = this.handleChanging.bind(this);

    this.state = {
      value: .5
    };
  }

  handleChanging(nextValue) {
    this.setState(() => ({
      value: nextValue
    }));
  }

  handleChange(nextValue) {
    this.setState(() => ({
      value: nextValue
    }));
  }

  render() {
    const {
      props: { className },
      state: { value }
    } = this;

    return (
      <div
        className={ classNames(
          ROOT_CSS + '',
          (className || '') + ''
        ) }
      >
        <Slider
          className="va__slider"
          onChanging={ this.handleChanging }
          onChange={ this.handleChange }
          value={ value }
        />
        <div className="va__value">{ ~~(value * 100) }%</div>
      </div>
    );
  }
}
