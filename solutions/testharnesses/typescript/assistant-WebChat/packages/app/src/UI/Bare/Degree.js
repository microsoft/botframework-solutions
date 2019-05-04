import React from 'react';

export default ({
  className,
  value
}) =>
  <span className={ className }>{ Math.floor(value) }&deg;{ ((value - Math.floor(value)) * 60).toFixed(3) }&prime;</span>
