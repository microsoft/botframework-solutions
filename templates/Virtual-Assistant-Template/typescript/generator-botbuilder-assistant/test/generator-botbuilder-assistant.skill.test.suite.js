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
const languages = [`zh`, `de`, `en`, `fr`, `it`, `es`];
const someLanguages = [`zh`, `de`, `en`];
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
    const dialogBotPath = join(`src`, `bots`, `dialogBot.ts`);
    const mainDialogPath = join(`src`, `dialogs`, `mainDialog.ts`);
    const skillDialogBasePath = join(`src`, `dialogs`, `skillDialogBase.ts`);
    const testCognitiveModelsPath = join(`test`, `mocks`, `resources`, `cognitiveModels.json`);

    const templatesFiles = [
        `package.json`,
        `.gitignore`,
        `.npmrc`,
        dialogBotPath,
        mainDialogPath,
        skillDialogBasePath,
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
        });

        after(function() {
            rimraf(join(__dirname, `tmp`, `**`), function () {});
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

        describe(`and have in the dialogBot file`, function() {
            it(`a private property with the given name`, function(done) {
                assert.fileContent(
                  join(skillGenerationPath, skillName, dialogBotPath),
                  `private readonly solutionName: string = '${skillNameCamelCase}';`
                );
                done();
              });
        });       
        
        describe(`and have in the mainDialog file`, function() {
            it(`a private property with the given name`, function(done) {
                assert.fileContent(
                  join(skillGenerationPath, skillName, mainDialogPath),
                  `private readonly solutionName: string = '${skillNameCamelCase}';`
                );
                done();
              });
        });
        
        describe(`and have in the skillDialogBase file`, function() {
            it(`a private property with the given name`, function(done) {
                assert.fileContent(
                  join(skillGenerationPath, skillName, skillDialogBasePath),
                  `private readonly solutionName: string = '${skillNameCamelCase}';`
                );
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