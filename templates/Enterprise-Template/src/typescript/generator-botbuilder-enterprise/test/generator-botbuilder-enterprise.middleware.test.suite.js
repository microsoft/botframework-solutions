"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");

describe("The generator-botbuilder-enterprise middleware tests ", () => {
  var middlewareName = "customMiddleware";
  const middlewareNameCamelCase = _camelCase(middlewareName);
  const middlewareNamePascalCase = _upperFirst(_camelCase(middlewareName));
  const middlewareGenerationPath = path.join("tmp", middlewareName);

  before(() => {
    return helpers
      .run(path.join(__dirname, "../generators/middleware"))
      .inDir(path.join(__dirname, "tmp"))
      .withPrompts({
        middlewareName: middlewareName,
        confirmationPath: true,
        middlewarePath: process.cwd(),
        finalConfirmation: true
      });
  });

  after(() => {
    rimraf.sync(path.join(__dirname, "tmp/*"));
  });

  describe("should create", () => {
    const files = [middlewareNameCamelCase + ".ts"];

    files.forEach(fileName =>
      it(fileName + " file", () => {
        assert.file(path.join(__dirname, middlewareGenerationPath, fileName));
      })
    );
  });

  describe("should have in the middleware file with the given name", () => {
    it("an export component with the given name", () => {
      assert.fileContent(
        path.join(
          __dirname,
          middlewareGenerationPath,
          middlewareNameCamelCase + ".ts"
        ),
        `export class ${middlewareNamePascalCase}`
      );
    });
  });
});
