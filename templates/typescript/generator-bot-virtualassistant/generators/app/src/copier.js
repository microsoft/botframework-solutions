/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
"use strict";
const { join } = require(`path`);
const templateFiles = new Map();
const deploymentTemplateFiles = new Map();
const allLanguages = [`zh-cn`, `de-de`, `en-us`, `fr-fr`, `it-it`, `es-es`];
let ignoredLanguages = [];
class Copier {
  // Constructor
  constructor(generator) {
    this.generator = generator;
  }

  // Copy the template ignoring the templates files
  copyTemplate(srcFolder, dstFolder) {
    this.generator.fs.copy(
      this.generator.templatePath(srcFolder),
      this.generator.destinationPath(dstFolder),
      {
        globOptions: {
          ignore: [...ignoredLanguages]
        }
      }
    );
  }

  // Copy the templates files passing the attributes of the new assistant and remove the files starting with character "_"
  copyTemplateFiles(srcFolder, dstFolder, newAssistant) {
    this.loadTemplatesFiles(newAssistant);
    templateFiles.forEach((dstFile, srcFile) => {
      this.generator.fs.copyTpl(
        this.generator.templatePath(srcFolder, srcFile),
        this.generator.destinationPath(dstFolder, dstFile),
        newAssistant
      );
      this.generator.fs.delete(join(dstFolder, srcFile));
    });
    deploymentTemplateFiles.forEach((dstFile, srcFile) => {
      this.generator.fs.copyTpl(
        this.generator.templatePath(srcFolder, srcFile),
        this.generator.destinationPath(dstFolder, dstFile),
        newAssistant
      );
    });
  }

  selectLanguages(languages) {
    // Take all the languages that will be ignored
    ignoredLanguages = allLanguages
      .filter(language => {
        return !languages.includes(language);
      })
      .map(language => {
        return this.pathToLUFolder(language);
      });

    // Add the paths of the deployment languages
    languages.forEach(language => {
      deploymentTemplateFiles.set(
        join(`deployment`, `resources`, `LU`, language),
        join(`deployment`, `resources`, `LU`, language)
      );
      deploymentTemplateFiles.set(
        join(`deployment`, `resources`, `QnA`, language),
        join(`deployment`, `resources`, `QnA`, language)
      );
    });
  }

  // Here you have to add the paths of your templates files
  loadTemplatesFiles(newAssistant) {
    templateFiles.set(`_package.json`, `package.json`);
    templateFiles.set(`_.eslintrc.json`, `.eslintrc.json`);
    templateFiles.set(`_.eslintignore`, `.eslintignore`);
    templateFiles.set(`_.gitignore`, `.gitignore`);
    templateFiles.set(`_.nycrc`, `.nycrc`);
    templateFiles.set(`_.npmrc`, `.npmrc`);
    templateFiles.set(
      join(`pipeline`, `_sample-assistant.yml`),
      join(`pipeline`, `${newAssistant.assistantName}.yml`)
    );
  }

  pathToLUFolder(language) {
    // Return join(`**`,`LU`, language, `*`);
    return join(`**`, language, `*.*`);
  }
}

exports.Copier = Copier;
