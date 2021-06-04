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
let selectedLanguages = [];

class Copier {
  // Constructor
  constructor(generator) {
    this.generator = generator;
  }

  // Copy the template ignoring the templates files, those that starts with _ character
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

  // Copy the templates files passing the attributes of the new skill
  copyTemplateFiles(srcFolder, dstFolder, newSkill) {
    this.loadTemplatesFiles(newSkill);
    templateFiles.forEach((dstFile, srcFile) => {
      this.generator.fs.copyTpl(
        this.generator.templatePath(srcFolder, srcFile),
        this.generator.destinationPath(dstFolder, dstFile),
        newSkill
      );
      this.generator.fs.delete(join(dstFolder, srcFile));
    });
    deploymentTemplateFiles.forEach((dstFile, srcFile) => {
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
    languages.forEach(language => {
      deploymentTemplateFiles.set(
        join(`deployment`, `resources`, `LU`, language, `general.lu`),
        join(`deployment`, `resources`, `LU`, language, `general.lu`)
      );
    });
  }

  // Here you have to add the paths of your templates files
  loadTemplatesFiles(newSkill) {
    templateFiles.set(`_package.json`, `package.json`);
    templateFiles.set(`_.eslintrc.json`, `.eslintrc.json`);
    templateFiles.set(`_.gitignore`, `.gitignore`);
    templateFiles.set(`_.nycrc`, `.nycrc`);
    templateFiles.set(`_.npmrc`, `.npmrc`);
    templateFiles.set(
      join(`pipeline`, `_sample-skill.yml`),
      join(`pipeline`, `${newSkill.skillName}.yml`)
    );
    templateFiles.set(
      join(`src`, `_cognitivemodels.json`),
      join(`src`, `cognitivemodels.json`)
    );
    templateFiles.set(
      join(`src`, `dialogs`, `_mainDialog.ts`),
      join(`src`, `dialogs`, `mainDialog.ts`)
    );
    templateFiles.set(
      join(`src`, `manifest`, `_manifest-1.0.json`),
      join(`src`, `manifest`, `manifest-1.0.json`)
    );
    templateFiles.set(
      join(`src`, `manifest`, `_manifest-1.1.json`),
      join(`src`, `manifest`, `manifest-1.1.json`)
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
