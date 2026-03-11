const commonjs = require('@rollup/plugin-commonjs');
const { nodeResolve } = require('@rollup/plugin-node-resolve');
const json = require('@rollup/plugin-json');
const path = require('path');

const RULESYNC_SUBMODULE = path.join(__dirname, 'rulesync');

module.exports = {
  input: path.join(RULESYNC_SUBMODULE, 'dist', 'cli', 'index.js'),
  output: {
    file: path.join(__dirname, 'native-binaries', 'rulesync-rollup.cjs'),
    format: 'cjs',
    exports: 'auto'
  },
  plugins: [
    json(),
    nodeResolve({
      preferBuiltins: true,
      modulesOnly: false
    }),
    commonjs({
      transformMixedEsModules: true,
      dynamicRequireTargets: [
        '**/jsonc-parser/**/*.js'
      ]
    })
  ],
  external: [] // Bundle everything
};
