const OVERRIDE_LANGUAGE_CODE = 'OVERRIDE_LANGUAGE_CODE';

export default function (languageCode) {
  return {
    type: OVERRIDE_LANGUAGE_CODE,
    payload: { languageCode }
  };
}

export { OVERRIDE_LANGUAGE_CODE }
