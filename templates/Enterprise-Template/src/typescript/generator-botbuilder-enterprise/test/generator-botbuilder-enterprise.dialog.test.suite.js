"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");

describe("The generator-botbuilder-enterprise dialog tests ", () => {
  var dialogName = "customDialog";
  var responsesName = "customResponses";
  const dialogNameCamelCase = _camelCase(dialogName);
  const dialogNamePascalCase = _upperFirst(_camelCase(dialogName));
  const responsesNameCamelCase = _camelCase(responsesName);
  const responsesNamePascalCase = _upperFirst(_camelCase(responsesName));
  const dialogGenerationPath = path.join("tmp", dialogName);

  before(() => {
    return helpers
      .run(path.join(__dirname, "../generators/dialog"))
      .inDir(path.join(__dirname, "tmp"))
      .withPrompts({
        dialogName: dialogName,
        confirmationPath: true,
        dialogPath: process.cwd(),
        finalConfirmation: true
      });
  });

  after(() => {
    rimraf.sync(path.join(__dirname, "tmp/*"));
  });

  describe("should create", () => {
    const files = [dialogNameCamelCase + ".ts", responsesNameCamelCase + ".ts"];

    files.forEach(fileName =>
      it(fileName + " file", () => {
        assert.file(path.join(__dirname, dialogGenerationPath, fileName));
      })
    );
  });

  describe("should have in the dialog file with the given name", () => {
    it("an import component containing the given name", () => {
      assert.fileContent(
        path.join(__dirname, dialogGenerationPath, dialogNameCamelCase + ".ts"),
        `import { ${responsesNamePascalCase} } from './${responsesNameCamelCase}'`
      );
    });

    it("an export component with the given name", () => {
      assert.fileContent(
        path.join(__dirname, dialogGenerationPath, dialogNameCamelCase + ".ts"),
        `export class ${dialogNamePascalCase}`
      );
    });

    it("an initialized attribute with the given name", () => {
      assert.fileContent(
        path.join(__dirname, dialogGenerationPath, dialogNameCamelCase + ".ts"),
        `RESPONDER: ${responsesNamePascalCase} = new ${responsesNamePascalCase}()`
      );
    });

    it("a super method with the given name as parameter", () => {
      assert.fileContent(
        path.join(__dirname, dialogGenerationPath, dialogNameCamelCase + ".ts"),
        `super(${dialogNamePascalCase}.name)`
      );
    });
  });

  describe("should have in the responses file with the given name", () => {
    it("an export component with the given name", () => {
      assert.fileContent(
        path.join(
          __dirname,
          dialogGenerationPath,
          responsesNameCamelCase + ".ts"
        ),
        `export class ${responsesNamePascalCase}`
      );
    });

    it("a parameter with the given name", () => {
      assert.fileContent(
        path.join(
          __dirname,
          dialogGenerationPath,
          responsesNameCamelCase + ".ts"
        ),
        `new DictionaryRenderer(${responsesNamePascalCase}.RESPONSE_TEMPLATES)`
      );
    });
  });
});
