export default {
  extends: ['stylelint-config-standard'],
  ignoreFiles: ['dist/**', 'coverage/**', '.angular/**'],
  rules: {
    'color-hex-length': 'short',
    'declaration-block-no-redundant-longhand-properties': true,
    'selector-class-pattern': [
      '^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$',
      {
        message: 'Use kebab-case CSS class names.',
      },
    ],
  },
};
