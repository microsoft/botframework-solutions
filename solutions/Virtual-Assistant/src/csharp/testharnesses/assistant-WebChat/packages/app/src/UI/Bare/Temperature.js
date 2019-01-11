import React from 'react';

export default ({
  celsius,
  className,
  unit = 'celsius'
}) =>
  typeof celsius === 'number' && (
    unit === 'fahrenheit' ?
      <span className={ className }>
        { Math.round(32 + celsius * 1.8) }&deg;F
      </span>
    :
      <span className={ className }>
        { celsius.toFixed(1) }&deg;C
      </span>
  )
