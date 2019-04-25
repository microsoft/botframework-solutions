/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
"use strict";
const path = require(`path`);
const templateFiles = new Map();
const allLanguages = [`zh`, `de`, `en`, `fr`, `it`, `es`];
let selectedLanguages = [];
let ignoredLanguages = [];

class Copier {
  // Constructor
  constructor(generator) {
    this.generator = generator;
  }

  // Copy the template ignoring the templates files, those that starts with _ character
  copyIgnoringTemplateFiles(srcFolder, dstFolder) {
    this.generator.fs.copy(
      this.generator.templatePath(srcFolder),
      this.generator.destinationPath(dstFolder),
      {
        globOptions: {
          ignore: ["**/_*.*", ...ignoredLanguages]
        }
      }
    );
  }

  // Copy the templates files passing the attributes of the new assistant
  copyTemplateFiles(srcFolder, dstFolder, newAssistant) {
    this.loadTemplatesFiles();
    templateFiles.forEach((dstFile, srcFile) => {
      this.generator.fs.copyTpl(
        this.generator.templatePath(srcFolder, srcFile),
        this.generator.destinationPath(dstFolder, dstFile),
        newAssistant
      );
    });
  }

  selectLanguages(languages) {
    // Take all the languages that will be ignored
    ignoredLanguages = allLanguages.filter(language => {
      return !languages.includes(language)
    }).map(language => {
      return this.pathToLUFolder(language);
    });

    // Add the paths of the deployment languages
    languages.forEach(language => {
      templateFiles.set(
        path.join(`deployment`,`resources`,`LU`, language),
        path.join(`deployment`,`resources`,`LU`, language)
      );
      templateFiles.set(
        path.join(`deployment`,`resources`,`QnA`, language),
        path.join(`deployment`,`resources`,`QnA`, language)
      );
      templateFiles.set(
        path.join(`deployment`,`resources`,`QnA`, language),
        path.join(`deployment`,`resources`,`QnA`, language)
      );
    });
  }

  // Here you have to add the paths of your templates files
  loadTemplatesFiles() {
    templateFiles.set(`_package.json`, `package.json`);
    templateFiles.set(`_.gitignore`, `.gitignore`);
    templateFiles.set(`_.npmrc`, `.npmrc`);
    templateFiles.set(path.join(`src`, `bots`, `_dialogBot.ts`), path.join(`src`, `bots`, `dialogBot.ts`))
  }

  pathToLUFolder(language){
    // return path.join(`**`,`LU`, language, `*`);
    return path.join(`**`, language, `*.*`);
  }
}

exports.Copier = Copier;
