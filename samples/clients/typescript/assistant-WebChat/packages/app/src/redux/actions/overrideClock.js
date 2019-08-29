const OVERRIDE_CLOCK = 'OVERRIDE_CLOCK';

export default function (date) {
  return {
    type: OVERRIDE_CLOCK,
    payload: { date }
  };
}

export { OVERRIDE_CLOCK }
