import setClock from '../actions/setClock';
import setTimezone from '../actions/setTimezone';

const RESOLUTION = 60000;

export default function () {
  return store => {
    let lastClock = 0;
    let lastTimezoneName = null;

    const refreshClock = () => {
      const now = new Date();
      const clock = Math.floor(now.getTime() / RESOLUTION) * RESOLUTION;

      if (clock !== lastClock) {
        store.dispatch(setClock(clock));
        lastClock = clock;
      }

      const timezoneOffset = now.getTimezoneOffset();
      let timezoneName;

      switch (timezoneOffset) {
        case -480:
          timezoneName = 'CST';
          break;

        default:
          timezoneName = 'PST';
          break;
      }

      if (timezoneName !== lastTimezoneName) {
        store.dispatch(setTimezone(timezoneName, timezoneOffset));
        lastTimezoneName = timezoneName;
      }
    };

    setImmediate(refreshClock);
    setInterval(refreshClock, 1000);

    return next => action => next(action);
  };
}
