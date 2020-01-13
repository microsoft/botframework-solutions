module.exports = {
    parser: '@typescript-eslint/parser',  // Specifies the ESLint parser
    plugins: [
        '@typescript-eslint'
    ],
    extends: [
        'plugin:@typescript-eslint/recommended'
    ],
    parserOptions: {
        ecmaVersion: 2018,  // Allows for the parsing of modern ECMAScript features
        sourceType: 'module'  // Allows for the use of imports
    },
    rules: {
        'no-unused-vars': 'off',
        '@typescript-eslint/no-unused-vars': ['error', {
          'args': 'none'
        }],
        '@typescript-eslint/no-use-before-define': 'off',
        '@typescript-eslint/no-namespace': 'off',
        '@typescript-eslint/no-inferrable-types': 'off',
        '@typescript-eslint/ban-types': 'off',
        '@typescript-eslint/interface-name-prefix': [ 'error', 'always' ],
        '@typescript-eslint/no-angle-bracket-type-assertion': 'off',
    },
};