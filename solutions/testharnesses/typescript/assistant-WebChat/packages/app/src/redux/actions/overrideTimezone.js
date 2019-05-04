const OVERRIDE_TIMEZONE = 'OVERRIDE_TIMEZONE';

export default function (name, offset) {
  return {
    type: OVERRIDE_TIMEZONE,
    payload: { offset, name }
  };
}

export { OVERRIDE_TIMEZONE }
