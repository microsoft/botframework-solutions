import setLatLong from '../actions/setLatLong';

export default function () {
  return store => {
    window.navigator.geolocation.getCurrentPosition(position => {
      const { latitude, longitude } = position.coords;

      store.dispatch(setLatLong(latitude, longitude));
    }, err => {
    }, { enableHighAccuracy: true });

    return next => action => next(action);
  };
}
