"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const _kebabCase = require("lodash/kebabCase");
const semver = require('semver');

describe("The generator-botbuilder-enterprise tests", function() {
  var botName;
  var botDesc;
  var botLang;
  var botNamePascalCase;
  var botNameCamelCase;
  var botGenerationPath;
  var pathConfirmation;
  var finalConfirmation;
  var run = true;
  const cognitiveDirectories = ["LUIS", "QnA"];
  const commonDirectories = [
    "dialogs",
    "extensions",
    "locales",
    "middleware",
    "serviceClients"
  ];
  const commonTestDirectories = [
    "flow"
  ];
  const rootFiles = [
    ".env.development",
    ".env.production",
    ".gitignore",
    "README.md",
    "tsconfig.json",
    "deploymentScripts/webConfigPrep.js",
    "package.json",
    "tslint.json"
  ];
  const testRootFiles = [
    ".env.test",
    "mocha.opts",
    "mockedConfiguration.bot",
    "testBase.js",
    "flow/botTestBase.js"
  ];

  describe("should create", function() {
    botName = "myBot";
    botDesc = "A description for myBot";
    botLang = "en";
    botName = _kebabCase(botName).replace(/([^a-z0-9-]+)/gi, "");
    botNamePascalCase = _upperFirst(_camelCase(botName));
    botNameCamelCase = _camelCase(botName);
    botGenerationPath = path.join(__dirname, "tmp");
    pathConfirmation = true;
    finalConfirmation = true;
    const srcFiles = [botNameCamelCase + ".ts", "botServices.ts"];

    before(async function() {
      await helpers
        .run(path.join(__dirname, "..", "generators", "app"))
        .inDir(botGenerationPath)
        .withArguments([
          "-n",
          botName,
          "-d",
          botDesc,
          "-l",
          botLang,
          "-p",
          botGenerationPath,
          "--noPrompt"
        ])
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp", "*"));
    });

    describe("the base", function() {
      it(botName + " folder", function(done) {
        assert.file(path.join(botGenerationPath, botName));
        done();
      });
    });

    describe("the folders", function() {
      commonDirectories.forEach(directoryName =>
        it(directoryName + " folder", function(done) {
          assert.file(
            path.join(botGenerationPath, botName, "src", directoryName)
          );
          done();
        })
      );
   
      cognitiveDirectories.forEach(directoryName =>
        it(directoryName + " folder", function(done) {
          assert.file(
            path.join(
              botGenerationPath,
              botName,
              "cognitiveModels",
              directoryName
            )
          );
          done();
        })
      );

      commonTestDirectories.forEach(testDirectoryName =>
        it(testDirectoryName + " folder", function(done) {
          assert.file(
            path.join(
              botGenerationPath,
              botName,
              "test",
              testDirectoryName
              )
          );
          done();
        })
      );
    });

    describe("the languages", function() {
      cognitiveDirectories.forEach(directoryName =>
        it("language '" + botLang + "' folder in " + directoryName, () => {
          assert.file(
            path.join(
              botGenerationPath,
              botName,
              "cognitiveModels",
              directoryName,
              botLang
            )
          );
        })
      );
  
      it("language '" + botLang + "' folder in deploymentScript", () => {
        assert.file(
          path.join(botGenerationPath, botName, "deploymentScripts", botLang)
        );
      });
  
      it("language '" + botLang + "' file in locales", () => {
        assert.file(
          path.join(
            botGenerationPath,
            botName,
            "src",
            "locales",
            botLang + ".json"
          )
        );
      });
    });

    describe("in the root folder", function() {
      rootFiles.forEach(fileName =>
        it(fileName + " file", function(done) {
          assert.file(path.join(botGenerationPath, botName, fileName));
          done();
        })
      );
    });

    describe("in the src folder", function() {
      
      srcFiles.forEach(fileName =>
        it(fileName + " file", function(done) {
          assert.file(path.join(botGenerationPath, botName, "src", fileName));
          done();
        })
      );
    });

    describe("in the test folder", function() {
      
      testRootFiles.forEach(testRootFileName =>
        it(testRootFileName + " file", function(done) {
          assert.file(path.join(botGenerationPath, botName, "test", testRootFileName));
          done();
        })
      );
    });

    describe("and have in the package.json", function() {
      it("a name property with the given name", function(done) {
        assert.fileContent(
          path.join(botGenerationPath, botName, "package.json"),
          `"name": "${botName}"`
        );
        done();
      });

      it("a description property with given description", function(done) {
        assert.fileContent(
          path.join(botGenerationPath, botName, "package.json"),
          `"description": "${botDesc}"`
        );
        done();
      });
    });

    describe("and have in the index file", function() {
      it("an import component containing the given name", function(done) {
        assert.fileContent(
          path.join(botGenerationPath, botName, "src", "index.ts"),
          `import { ${botNamePascalCase} } from './${botNameCamelCase}'`
        );
        done();
      });

      it("a declaration component with the given name", function(done) {
        assert.fileContent(
          path.join(botGenerationPath, botName, "src", "index.ts"),
          `let bot: ${botNamePascalCase}`
        );
        done();
      });

      it("an instantiation component with the given name", function(done) {
        assert.fileContent(
          path.join(botGenerationPath, botName, "src", "index.ts"),
          `bot = new ${botNamePascalCase}`
        );
        done();
      });
    });

    describe("and have in the file with the given name", function() {
      it("an export component with the given name", function(done) {
        assert.fileContent(
          path.join(
            botGenerationPath,
            botName,
            "src",
            botNameCamelCase + ".ts"
          ),
          `export class ${botNamePascalCase}`
        );
        done();
      });

      it("a parameter component with the given name", function(done) {
        assert.fileContent(
          path.join(
            botGenerationPath,
            botName,
            "src",
            botNameCamelCase + ".ts"
          ),
          `('${botNamePascalCase}')`
        );
        done();
      });
    });

    describe("and have in the bot test base file", function() {
      it("an import component with the given name", function(done) {
        assert.fileContent(
          path.join(
            botGenerationPath,
            botName,
            "test",
            "flow",
            "botTestBase.js"
          ),
          `const ${botNamePascalCase} = require('../../lib/${botNameCamelCase}.js').${botNamePascalCase};`
        );
        done();
      });

      it("a variable with the type of the given name", function(done) {
        assert.fileContent(
          path.join(
            botGenerationPath,
            botName,
            "test",
            "flow",
            "botTestBase.js"
          ),
          `this.bot = new ${botNamePascalCase}(services, conversationState, userState);`
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
          .run(path.join(__dirname, "..", "generators", "app"))
          .inDir(botGenerationPath)
          .withPrompts({
            botName: botName,
            botDesc: botDesc,
            botLang: botLang,
            pathConfirmation: pathConfirmation,
            botGenerationPath: botGenerationPath,
            finalConfirmation: finalConfirmation
          });
      }
    });

    after(function() {
      rimraf.sync(path.join(__dirname, "tmp", "*"));
    });

    describe("the base", function() {
      it(botName + " folder when the final confirmation is deny", function(done) {
        if(!run){
          this.skip()          
        }
        assert.noFile(botGenerationPath, botName);
        done();
      });
    });
  });
});
