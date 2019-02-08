"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const semver = require('semver');

describe("The generator-botbuilder-enterprise dialog tests", function() {
  var dialogName;
  var responsesName;
  var dialogNameCamelCase;
  var dialogNamePascalCase;
  var responsesNameCamelCase;
  var responsesNamePascalCase;
  var dialogGenerationPath;
  var pathConfirmation;
  var finalConfirmation;
  var run = true;

  describe("should create", function() {
    dialogName = "customDialog";
    responsesName = "customResponses";
    dialogNameCamelCase = _camelCase(dialogName);
    dialogNamePascalCase = _upperFirst(_camelCase(dialogName));
    responsesNameCamelCase = _camelCase(responsesName);
    responsesNamePascalCase = _upperFirst(_camelCase(responsesName));
    dialogGenerationPath = path.join(__dirname,"tmp");
    pathConfirmation = true;
    finalConfirmation = true;

    before(async function() {
      await helpers
        .run(path.join(__dirname, "..", "generators", "dialog"))
        .inDir(dialogGenerationPath)
        .withArguments([
          '-n',
          dialogName,
          '-p',
          dialogGenerationPath,
          '--noPrompt'
        ])
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp", "*"));
    });

    describe("the files", function() {
      const files = [
        dialogNameCamelCase + ".ts",
        responsesNameCamelCase + ".ts"
      ];

      files.forEach(fileName =>
        it(fileName + " file", function(done) {
          assert.file(path.join(dialogGenerationPath, dialogName, fileName));
          done();
        })
      );
    });

    describe("and have in the dialog file with the given name", function() {
      it("an import component containing the given name", function(done) {
        assert.fileContent(
          path.join(
            dialogGenerationPath,
            dialogName,
            dialogNameCamelCase + ".ts"
          ),
          `import { ${responsesNamePascalCase} } from './${responsesNameCamelCase}'`
        );
        done();
      });

      it("an export component with the given name", function(done) {
        assert.fileContent(
          path.join(
            dialogGenerationPath,
            dialogName,
            dialogNameCamelCase + ".ts"
          ),
          `export class ${dialogNamePascalCase}`
        );
        done();
      });

      it("an initialized attribute with the given name", function(done) {
        assert.fileContent(
          path.join(
            dialogGenerationPath,
            dialogName,
            dialogNameCamelCase + ".ts"
          ),
          `RESPONDER: ${responsesNamePascalCase} = new ${responsesNamePascalCase}()`
        );
        done();
      });

      it("a super method with the given name as parameter", function(done) {
        assert.fileContent(
          path.join(
            dialogGenerationPath,
            dialogName,
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
            dialogGenerationPath,
            dialogName,
            responsesNameCamelCase + ".ts"
          ),
          `export class ${responsesNamePascalCase}`
        );
        done();
      });

      it("a parameter with the given name", function(done) {
        assert.fileContent(
          path.join(
            dialogGenerationPath,
            dialogName,
            responsesNameCamelCase + ".ts"
          ),
          `new DictionaryRenderer(${responsesNamePascalCase}.RESPONSE_TEMPLATES)`
        );
        done();
      });
    });
  });

  describe("should not create", function() {
    before(async function() {
      if(semver.gte(process.versions.node,'10.12.0')){
        run = false;
      }
      else{
      finalConfirmation = false;
      await helpers
        .run(path.join(__dirname, "..", "generators", "dialog"))
        .inDir(dialogGenerationPath)
        .withPrompts({
          dialogName: dialogName,
          pathConfirmation: pathConfirmation,
          dialogGenerationPath: dialogGenerationPath,
          finalConfirmation: finalConfirmation
        });
      }
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp", "*"));
    });

    describe("the base", function() {
      it(dialogName + " folder when the final confirmation is deny", function(done) {
        if(!run){
          this.skip()          
        }
        assert.noFile(path.join(dialogGenerationPath, dialogName));
        done();
      });
    });
  });
});
