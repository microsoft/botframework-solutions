const SET_NAVIGATION_DESTINATION = 'SET_NAVIGATION_DESTINATION';

export default function (name, address) {
  return {
    type: SET_NAVIGATION_DESTINATION,
    payload: {
      address,
      name
    }
  };
}

export { SET_NAVIGATION_DESTINATION }
