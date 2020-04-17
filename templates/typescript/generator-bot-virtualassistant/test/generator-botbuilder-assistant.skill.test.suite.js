/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
*/
"use strict";
const assert = require(`yeoman-assert`);
const helpers = require(`yeoman-test`);
const rimraf = require(`rimraf`);
const _kebabCase = require(`lodash/kebabCase`);
const _camelCase = require(`lodash/camelCase`);
const semver = require('semver');
const languages = [`zh-cn`, `de-de`, `en-us`, `fr-fr`, `it-it`, `es-es`];
const someLanguages = [`zh-cn`, `de-de`, `en-us`];
const { join } = require(`path`);
const sinon = require(`sinon`);

describe(`The generator-botbuilder-assistant skill tests`, function() {
    var skillName;
    var skillNameCamelCase;
    var skillDesc;
    var pathConfirmation;
    var skillGenerationPath;
    var finalConfirmation;
    var run = true;
    var packageJSON;
    var manifestTemplate;
    const manifestTemplatePath = join(`src`, `manifestTemplate.json`);
    const testCognitiveModelsPath = join(`test`, `mocks`, `resources`, `cognitiveModels.json`);

    const templatesFiles = [
        `package.json`,
        `.eslintrc.json`,
        `.gitignore`,
        `.nycrc`,
        manifestTemplatePath,
        testCognitiveModelsPath
    ];

    const commonDirectories = [
        `deployment`,
        join(`deployment`, `resources`),
        join(`deployment`, `scripts`),
        `src`,
        join(`src`, `adapters`),
        join(`src`, `bots`),
        join(`src`, `dialogs`),
        join(`src`, `models`),
        join(`src`, `responses`),
        join(`src`, `services`),
        `test`,
        join(`test`, `helpers`),
        join(`test`, `mocks`),
        join(`test`, `mocks`, `resources`),
    ];

    describe(`should create`, function() {
        skillName = `sample-skill`;
        skillDesc = `A description for sample-skill`;
        skillName = _kebabCase(skillName).replace(
            /([^a-z0-9-]+)/gi,
            ``
        ); 

        skillNameCamelCase = _camelCase(skillName).replace(
            /([^a-z0-9-]+)/gi,
            ``
        ); 
        skillGenerationPath = join(__dirname, "tmp");
        pathConfirmation = true;
        finalConfirmation = true;
        const pipelinePath = join(`pipeline`, `${skillName}.yml`);
        templatesFiles.push(pipelinePath);

        before(async function(){
            await helpers
            .run(join(__dirname, `..`, `generators`, `skill`))
            .inDir(skillGenerationPath)
            .withArguments([
              `-n`,
              skillName,
              `-d`,
              skillDesc,
              `-l`,
              someLanguages.join(`,`),
              `-p`,
              skillGenerationPath,
              `--noPrompt`
            ])
            .on('ready', generator => {
                generator.spawnCommandSync = sinon.spy();
            });;

            packageJSON = require(join(skillGenerationPath, skillName, `package.json`));
            manifestTemplate = require(join(skillGenerationPath, skillName, manifestTemplatePath));
        });

        after(function() {
            rimraf(join(__dirname, `tmp`, `**`), function () {});
            process.chdir(join(__dirname, `..`));
        });

        describe(`the base`, function() {
            it(skillName.concat(` folder`), function(done) {
                assert.file(
                    join(skillGenerationPath, skillName)
                );
                done();
            });
        });

        describe(`the folders`, function() {
            commonDirectories.forEach(directoryName => 
                it(directoryName.concat(" folder"), function(done) {
                    assert.file(
                        join(skillGenerationPath, skillName, directoryName)
                    )
                    done();
                })
            );
        });

        describe(`the files`, function() {
            templatesFiles.forEach(templateFile => 
                it(templateFile.concat(" file"), function(done) {
                    assert.file(
                        join(skillGenerationPath, skillName, templateFile)
                    )
                    done();
                })
            );
        });

        describe(`and have in the manifestTemplate.json`, function() {
            it(`an id property with the given name`, function(done) {
                assert.strictEqual(manifestTemplate.id, skillNameCamelCase);
                done();
            });

            it(`a name property with given name`, function(done) {
                assert.strictEqual(manifestTemplate.name, skillNameCamelCase);
                done();
            });

            it(`an id of an action property with given name`, function(done) {
                assert.strictEqual(manifestTemplate.actions[0].id, `${skillNameCamelCase}_Sample`);
                done();
            });

            it(`a description definition of an action property with given name`, function(done) {
                assert.strictEqual(manifestTemplate.actions[0].definition.description, `${skillNameCamelCase} action with no slots`);
                done();
            });

            it(`an utterance source with given name`, function(done) {
                assert.strictEqual(manifestTemplate.actions[0].definition.triggers.utteranceSources[0].source[0], `${skillNameCamelCase}#Sample`);
                done();
            });
        });
        
        describe(`and have in the package.json`, function() {
            it(`a name property with the given name`, function(done) {
                assert.strictEqual(packageJSON.name, skillName);
                done();
            });

            it(`a description property with given description`, function(done) {
                assert.strictEqual(packageJSON.description, skillDesc);
                done();
            });
        });
        
        describe(`and have in the cognitiveModels file`, function() {
            it(`an id property with the given name`, function(done) {
                assert.fileContent(
                  join(skillGenerationPath, skillName, testCognitiveModelsPath),
                  `"id": "${skillNameCamelCase}",`
                );
                done();
            });

            it(`a name property with the given name`, function(done) {
                assert.fileContent(
                  join(skillGenerationPath, skillName, testCognitiveModelsPath),
                  `"name": "${skillNameCamelCase}",`
                );
                done();
            });
        });
    })

    describe(`should not create`, function() {
        before(async function() {
            if(semver.gte(process.versions.node, `10.12.0`)){
                run = false;
            } else {
                finalConfirmation = true;
                await helpers
                    .run(join(
                        __dirname,
                        `..`,
                        `generators`,
                        `skill`
                    ))
                    .inDir(skillGenerationPath)
                    .withPrompts({
                        skillName: skillName,
                        skillDesc: skillDesc,
                        skillLang: languages,
                        pathConfirmation: pathConfirmation,
                        skillGenerationPath: skillGenerationPath,
                        finalConfirmation: finalConfirmation
                    })
                    .on('ready', generator => {
                        generator.spawnCommandSync = sinon.spy();
                    });;
            }
        });

        after(function() {
            rimraf(join(__dirname, `tmp`, `**`), function () {});
            process.chdir(join(__dirname, `..`));
        });

        describe(`the base`, function() {
            it(skillName + ` folder when the execution is skipped`, function(done) {
              if(!run){
                this.skip()          
              }
              assert.noFile(skillGenerationPath, skillName);
              done();
            });
        });
    })
});
