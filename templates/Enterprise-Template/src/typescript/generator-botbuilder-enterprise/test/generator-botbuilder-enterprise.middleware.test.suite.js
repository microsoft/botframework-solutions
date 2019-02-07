"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");

describe("The generator-botbuilder-enterprise middleware tests ", function() {
  var middlewareName;
  var middlewareNameCamelCase;
  var middlewareNamePascalCase;
  var middlewareGenerationPath;
  var confirmationPath;
  var finalConfirmation;

  describe("should create", function() {
    middlewareName = "customMiddleware";
    middlewareNameCamelCase = _camelCase(middlewareName);
    middlewareNamePascalCase = _upperFirst(_camelCase(middlewareName));
    middlewareGenerationPath = path.join("tmp", middlewareName);
    confirmationPath = true;
    finalConfirmation = true;

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

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp/*"));
    });

    describe("the file", function() {
      const file = [middlewareNameCamelCase + ".ts"];

      file.forEach(fileName =>
        it(fileName + " file", function(done) {
          assert.file(path.join(__dirname, middlewareGenerationPath, fileName));
          done();
        })
      );
    });

    describe("and have in the middleware file with the given name", () => {
      it("an export component with the given name", function(done) {
        assert.fileContent(
          path.join(
            __dirname,
            middlewareGenerationPath,
            middlewareNameCamelCase + ".ts"
          ),
          `export class ${middlewareNamePascalCase}`
        );
        done();
      });
    });
  });

  describe("should not create", function() {
    before(function() {
      finalConfirmation = false;
      return helpers
        .run(path.join(__dirname, "../generators/middleware"))
        .inDir(path.join(__dirname, "tmp"))
        .withPrompts({
          middlewareName: middlewareName,
          confirmationPath: confirmationPath,
          middlewareGenerationPath: middlewareGenerationPath,
          finalConfirmation: finalConfirmation
        });
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp/*"));
    });

    describe("the base", function() {
      it(
        middlewareName + " folder when the final confirmation is deny",
        function(done) {
          assert.noFile(path.join(__dirname, middlewareGenerationPath));
          done();
        }
      );
    });
  });
});
