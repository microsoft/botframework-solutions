const SET_TEMPERATURE_UNIT = 'SET_TEMPERATURE_UNIT';

export default function (temperatureUnit) {
  return {
    type: SET_TEMPERATURE_UNIT,
    payload: {
      temperatureUnit: temperatureUnit === 'fahrenheit' ? 'fahrenheit' : 'celsius'
    }
  };
}

export { SET_TEMPERATURE_UNIT }
