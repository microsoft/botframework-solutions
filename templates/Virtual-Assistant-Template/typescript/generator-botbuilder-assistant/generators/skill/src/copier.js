/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
"use strict";
const { join } = require(`path`);
const templateFiles = new Map();
const allLanguages = [`zh`, `de`, `en`, `fr`, `it`, `es`];
let ignoredLanguages = [];
let selectedLanguages = [];

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

  // Copy the templates files passing the attributes of the new skill
  copyTemplateFiles(srcFolder, dstFolder, newSkill) {
    this.loadTemplatesFiles(newSkill);
    templateFiles.forEach((dstFile, srcFile) => {
      this.generator.fs.copyTpl(
        this.generator.templatePath(srcFolder, srcFile),
        this.generator.destinationPath(dstFolder, dstFile),
        newSkill
      );
    });
  }

  selectLanguages(languages) {
    selectedLanguages = languages;
    // Take all the languages that will be ignored
    ignoredLanguages = allLanguages
      .filter(language => {
        return !selectedLanguages.includes(language);
      })
      .map(language => {
        return this.pathToLUFolder(language);
      });

    // Add the paths of the deployment languages
    selectedLanguages.forEach(language => {
      templateFiles.set(
        join(`deployment`, `resources`, `LU`, language, `general.lu`),
        join(`deployment`, `resources`, `LU`, language, `general.lu`)
      );
    });
  }

  // Here you have to add the paths of your templates files
  loadTemplatesFiles(newSkill) {
    templateFiles.set(`_package.json`, `package.json`);
    templateFiles.set(`_.eslintrc.js`, `.eslintrc.js`);
    templateFiles.set(`_.gitignore`, `.gitignore`);
    templateFiles.set(`_.npmrc`, `.npmrc`);
    templateFiles.set(`_.nycrc`, `.nycrc`);
    templateFiles.set(
      join(`pipeline`, `_sample-skill.yml`),
      join(`pipeline`, `${newSkill.skillName}.yml`)
    );
    templateFiles.set(
      join(`src`, `bots`, `_dialogBot.ts`),
      join(`src`, `bots`, `dialogBot.ts`)
    );
    templateFiles.set(
      join(`src`, `_manifestTemplate.json`),
      join(`src`, `manifestTemplate.json`)
    );
    templateFiles.set(
      join(`src`, `dialogs`, `_mainDialog.ts`),
      join(`src`, `dialogs`, `mainDialog.ts`)
    );
    templateFiles.set(
      join(`src`, `dialogs`, `_skillDialogBase.ts`),
      join(`src`, `dialogs`, `skillDialogBase.ts`)
    );
    templateFiles.set(
      join(`test`, `mocks`, `resources`, `_cognitiveModels.json`),
      join(`test`, `mocks`, `resources`, `cognitiveModels.json`)
    );
    selectedLanguages.forEach(language => {
      templateFiles.set(
        join(`deployment`, `resources`, `LU`, language, `_skill.lu`),
        join(
          `deployment`,
          `resources`,
          `LU`,
          language,
          `${newSkill.skillNameCamelCase}.lu`
        )
      );
    });
  }

  pathToLUFolder(language) {
    // Return join(`**`,`LU`, language, `*`);
    return join(`**`, language, `*.*`);
  }
}

exports.Copier = Copier;
