const SET_TAB = 'SET_TAB';

export default function (tab) {
  return {
    type: SET_TAB,
    payload: { tab }
  };
}

export { SET_TAB }
