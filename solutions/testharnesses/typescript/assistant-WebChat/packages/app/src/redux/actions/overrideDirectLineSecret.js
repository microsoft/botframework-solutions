const OVERRIDE_DIRECT_LINE_SECRET = 'OVERRIDE_DIRECT_LINE_SECRET';

export default function (directLineSecret) {
  return {
    type: OVERRIDE_DIRECT_LINE_SECRET,
    payload: { directLineSecret }
  };
}

export { OVERRIDE_DIRECT_LINE_SECRET }
