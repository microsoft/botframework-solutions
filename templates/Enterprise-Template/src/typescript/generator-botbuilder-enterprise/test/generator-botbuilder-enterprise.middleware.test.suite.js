"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const semver = require('semver');

describe("The generator-botbuilder-enterprise middleware tests ", function() {
  var middlewareName;
  var middlewareNameCamelCase;
  var middlewareNamePascalCase;
  var middlewareGenerationPath;
  var pathConfirmation;
  var finalConfirmation;
  var run = true;

  describe("should create", function() {
    middlewareName = "customMiddleware";
    middlewareNameCamelCase = _camelCase(middlewareName);
    middlewareNamePascalCase = _upperFirst(_camelCase(middlewareName));
    middlewareGenerationPath = path.join(__dirname, "tmp");
    pathConfirmation = true;
    finalConfirmation = true;

    before(async function() {
      await helpers
        .run(path.join(__dirname, "..", "generators", "middleware"))
        .inDir(middlewareGenerationPath)
        .withArguments([
          '-n',
          middlewareName,
          '-p',
          middlewareGenerationPath,
          '--noPrompt'
        ])
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp", "*"));
    });

    describe("the file", function() {
      const file = [middlewareNameCamelCase + ".ts"];

      file.forEach(fileName =>
        it(fileName + " file", function(done) {
          assert.file(path.join(middlewareGenerationPath, middlewareName, fileName));
          done();
        })
      );
    });

    describe("and have in the middleware file with the given name", function() {
      it("an export component with the given name", function(done) {
        assert.fileContent(
          path.join(
            middlewareGenerationPath,
            middlewareName,
            middlewareNameCamelCase + ".ts"
          ),
          `export class ${middlewareNamePascalCase}`
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
        .run(path.join(__dirname, "..", "generators", "middleware"))
        .inDir(middlewareGenerationPath)
        .withPrompts({
          middlewareName: middlewareName,
          pathConfirmation: pathConfirmation,
          middlewareGenerationPath: middlewareGenerationPath,
          finalConfirmation: finalConfirmation
        });
      }
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp", "*"));
    });

    describe("the base", function() {
      it(
        middlewareName + " folder when the final confirmation is deny",
        function(done) {
          if(!run){
            this.skip()          
          }
          assert.noFile(path.join(middlewareGenerationPath, middlewareName));
          done();
        }
      );
    });
  });
});
