"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");

describe("The generator-botbuilder-enterprise dialog tests", function() {
  var dialogName;
  var responsesName;
  var dialogNameCamelCase;
  var dialogNamePascalCase;
  var responsesNameCamelCase;
  var responsesNamePascalCase;
  var dialogGenerationPath;
  var confirmationPath;
  var finalConfirmation;

  describe("should create", function() {
    dialogName = "customDialog";
    responsesName = "customResponses";
    dialogNameCamelCase = _camelCase(dialogName);
    dialogNamePascalCase = _upperFirst(_camelCase(dialogName));
    responsesNameCamelCase = _camelCase(responsesName);
    responsesNamePascalCase = _upperFirst(_camelCase(responsesName));
    dialogGenerationPath = path.join("tmp", dialogName);
    confirmationPath = true;
    finalConfirmation = true;

    before(function() {
      return helpers
        .run(path.join(__dirname, "../generators/dialog"))
        .inDir(path.join(__dirname, "tmp"))
        .withPrompts({
          dialogName: dialogName,
          confirmationPath: confirmationPath,
          dialogGenerationPath: dialogGenerationPath,
          finalConfirmation: finalConfirmation
        });
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp/*"));
    });

    describe("the files", function() {
      const files = [
        dialogNameCamelCase + ".ts",
        responsesNameCamelCase + ".ts"
      ];

      files.forEach(fileName =>
        it(fileName + " file", function(done) {
          assert.file(path.join(__dirname, dialogGenerationPath, fileName));
          done();
        })
      );
    });

    describe("and have in the dialog file with the given name", function() {
      it("an import component containing the given name", function(done) {
        assert.fileContent(
          path.join(
            __dirname,
            dialogGenerationPath,
            dialogNameCamelCase + ".ts"
          ),
          `import { ${responsesNamePascalCase} } from './${responsesNameCamelCase}'`
        );
        done();
      });

      it("an export component with the given name", function(done) {
        assert.fileContent(
          path.join(
            __dirname,
            dialogGenerationPath,
            dialogNameCamelCase + ".ts"
          ),
          `export class ${dialogNamePascalCase}`
        );
        done();
      });

      it("an initialized attribute with the given name", function(done) {
        assert.fileContent(
          path.join(
            __dirname,
            dialogGenerationPath,
            dialogNameCamelCase + ".ts"
          ),
          `RESPONDER: ${responsesNamePascalCase} = new ${responsesNamePascalCase}()`
        );
        done();
      });

      it("a super method with the given name as parameter", function(done) {
        assert.fileContent(
          path.join(
            __dirname,
            dialogGenerationPath,
            dialogNameCamelCase + ".ts"
          ),
          `super(${dialogNamePascalCase}.name)`
        );
        done();
      });
    });

    describe("and have in the responses file with the given name", function() {
      it("an export component with the given name", function(done) {
        assert.fileContent(
          path.join(
            __dirname,
            dialogGenerationPath,
            responsesNameCamelCase + ".ts"
          ),
          `export class ${responsesNamePascalCase}`
        );
        done();
      });

      it("a parameter with the given name", function(done) {
        assert.fileContent(
          path.join(
            __dirname,
            dialogGenerationPath,
            responsesNameCamelCase + ".ts"
          ),
          `new DictionaryRenderer(${responsesNamePascalCase}.RESPONSE_TEMPLATES)`
        );
        done();
      });
    });
  });

  describe("should not create", function() {
    before(function() {
      finalConfirmation = false;
      return helpers
        .run(path.join(__dirname, "../generators/dialog"))
        .inDir(path.join(__dirname, "tmp"))
        .withPrompts({
          dialogName: dialogName,
          confirmationPath: confirmationPath,
          dialogGenerationPath: dialogGenerationPath,
          finalConfirmation: finalConfirmation
        });
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp/*"));
    });

    describe("the base", function() {
      it(dialogName + " folder when the final confirmation is deny", function(done) {
        assert.noFile(path.join(__dirname, dialogGenerationPath));
        done();
      });
    });
  });
});
