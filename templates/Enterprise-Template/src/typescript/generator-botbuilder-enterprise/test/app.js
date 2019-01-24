'use strict';
const path = require('path');
const assert = require('yeoman-assert');
const helpers = require('yeoman-test');
const rimraf = require('rimraf');
const _camelCase = require('lodash/camelCase');
const _upperFirst = require('lodash/upperFirst');
const _kebabCase = require('lodash/kebabCase');

describe('The generator-botbuilder-enterprise ', () => {
  var botName = "myBot";
  const botDesc = "A description for myBot" ;
  const botLang = "en";
  const botName_pascalCase = _upperFirst(_camelCase(botName));
  const botName_camelCase = _camelCase(botName);
  botName = _kebabCase(botName);

  before(() => {
    return helpers
      .run(path.join(__dirname, '../generators/app'))
      .inDir(path.join(__dirname, 'tmp'))
      .withPrompts({
        botName: botName,
        botDesc: botDesc,
        botLang: botLang,
        finalConfirmation: true
      });
  });

  after(() => {
    rimraf.sync(path.join(__dirname, 'tmp/*'));
  });

  describe('should create', () => {
    const commonDirectories = [ 'dialogs', 'extensions', 'locales', 'middleware', 'serviceClients' ];
    commonDirectories.forEach(directoryName =>
      it(directoryName + ' folder', () => {
        assert.file(path.join(__dirname, 'tmp/' + botName_camelCase + '/src/' + directoryName + '/'));
      }));
  
    const cognitiveDirectories = ['LUIS', 'QnA'];
    cognitiveDirectories.forEach( directoryName => 
      it(directoryName + ' folder', () => {
        assert.file(path.join(__dirname, 'tmp/' + botName_camelCase + '/cognitiveModels/'+ directoryName + '/'));
      }));
  });

  describe('should create in the root folder', () => {
    const rootFiles = ['.env.development', '.env.production', '.gitignore', 'README.md', 'tsconfig.json', 'deploymentScripts/webConfigPrep.js', 'package.json' ];
    rootFiles.forEach(fileName => 
      it(fileName + 'file', () => {
        assert.file(path.join(__dirname,'tmp/' + botName_camelCase + '/' + fileName));
      }));  
  });

  describe('should create in the src folder', () => {
    const srcFiles = [botName_camelCase + '.ts','botServices.ts'];
    srcFiles.forEach(fileName =>
      it(fileName + ' file', () => {
        assert.file(path.join(__dirname, 'tmp/' + botName_camelCase + '/src/' + fileName));
      }));
  });

  describe('should have in the package.json', () => {
    it('a name property with the given name', () => {
      assert.fileContent(path.join(__dirname, 'tmp/' + botName_camelCase + '/package.json'), `"name": "${botName}"`);
    });
  
    it('a description property with given description', () => {
      assert.fileContent(path.join(__dirname, 'tmp/' + botName_camelCase + '/package.json'), `"description": "${botDesc}"`);
    });
  });

  describe('should have in the index file', () => {
    it('an import component containing the given name', () => {
      assert.fileContent(path.join(__dirname, 'tmp/' + botName_camelCase + '/src/index.ts'), `import { ${botName_pascalCase} } from "./${botName_camelCase}"`);
    });
  
    it('a declaration component with the given name', () => {
      assert.fileContent(path.join(__dirname, 'tmp/' + botName_camelCase + '/src/index.ts'), `let bot: ${botName_pascalCase}`);
    });
  
    it('an instantiation component with the given name', () => {
      assert.fileContent(path.join(__dirname, 'tmp/' + botName_camelCase + '/src/index.ts'), `bot = new ${botName_pascalCase}`);
    });
  });


  describe('should have in the file with the given name', () => {
    it('an export component with the given name', () => {
      assert.fileContent(path.join(__dirname, 'tmp/' + botName_camelCase + '/src/' + botName_camelCase + '.ts'), `export class ${botName_pascalCase}`);
    });
  
    it('a parameter component with the given name', () => {
      assert.fileContent(path.join(__dirname, 'tmp/' + botName_camelCase + '/src/' + botName_camelCase + '.ts'), `("${botName_pascalCase}")`);
    });
  });
});