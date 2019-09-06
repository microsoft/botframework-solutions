import React from 'react';

export default ({
  className,
  kilometer,
  unit,
  unitText
}) =>
  unit === 'mile' ?
    <span className={ className }>{ Math.round(kilometer * 0.62137119) } { typeof unitText === 'string' ? unitText : 'Miles' }</span>
  :
    <span className={ className }>{ Math.round(kilometer) } { typeof unitText === 'string' ? unitText : 'KM' }</span>
