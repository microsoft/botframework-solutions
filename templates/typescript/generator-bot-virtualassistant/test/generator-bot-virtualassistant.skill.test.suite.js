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

describe(`The generator-bot-virtualassistant skill tests`, function() {
    var skillName;
    var skillNameCamelCase;
    var skillDesc;
    var pathConfirmation;
    var skillGenerationPath;
    var finalConfirmation;
    var run = true;
    var packageJSON;
    var manifest1_0;
    var manifest1_1;
    const manifestPath1_0 = join(`src`, `manifest`, `manifest-1.0.json`);
    const manifestPath1_1 = join(`src`, `manifest`, `manifest-1.1.json`);
    const mainDialogPath = join(`src`, `dialogs`, `mainDialog.ts`);
    const cognitiveModelsPath = join(`src`, `cognitiveModels.json`);    

    const templatesFiles = [
        `package.json`,
        `.eslintrc.json`,
        `.gitignore`,
        `.nycrc`,
        mainDialogPath,
        manifestPath1_0,
        manifestPath1_1,
        cognitiveModelsPath
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
            });

            packageJSON = require(join(skillGenerationPath, skillName, `package.json`));
            manifest1_0 = require(join(skillGenerationPath, skillName, manifestPath1_0));
            manifest1_1 = require(join(skillGenerationPath, skillName, manifestPath1_1));
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

        describe(`and have in the manifest-1.0.json`, function() {
            it(`an id property with the given name`, function(done) {
                assert.strictEqual(manifest1_0.$id, skillNameCamelCase);
                done();
            });

            it(`a name property with given name`, function(done) {
                assert.strictEqual(manifest1_0.name, skillNameCamelCase);
                done();
            });

            it(`a description with given name`, function(done) {
                assert.strictEqual(manifest1_0.description, `${skillNameCamelCase} description`);
                done();
            });

            it(`an iconUrl value with given name`, function(done) {
                assert.strictEqual(manifest1_0.iconUrl, `https://{YOUR_SKILL_URL}/${skillNameCamelCase}.png`);
                done();
            });

            it(`a description definition of an endpoint with given name`, function(done) {
                assert.strictEqual(manifest1_0.endpoints[0].description, `Production endpoint for the ${skillNameCamelCase}`);
                done();
            });
        });

        describe(`and have in the manifest-1.1.json`, function() {
            it(`an id property with the given name`, function(done) {
                assert.strictEqual(manifest1_1.$id, skillNameCamelCase);
                done();
            });

            it(`a name property with given name`, function(done) {
                assert.strictEqual(manifest1_1.name, skillNameCamelCase);
                done();
            });

            it(`a description with given name`, function(done) {
                assert.strictEqual(manifest1_1.description, `${skillNameCamelCase} description`);
                done();
            });

            it(`an iconUrl value with given name`, function(done) {
                assert.strictEqual(manifest1_1.iconUrl, `https://{YOUR_SKILL_URL}/${skillNameCamelCase}.png`);
                done();
            });

            it(`a description definition of an endpoint with given name`, function(done) {
                assert.strictEqual(manifest1_1.endpoints[0].description, `Production endpoint for the ${skillNameCamelCase}`);
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

        describe(`and have in the mainDialog file`, function() {
            it(`an line with the following structure`, function(done) {
                assert.fileContent(
                    join(skillGenerationPath, skillName, mainDialogPath),
                    `const skillLuis: LuisRecognizer | undefined = localizedServices.luisServices ? localizedServices.luisServices.get('${skillNameCamelCase}') : undefined;`
                );
                done();
            });
        });

        describe(`and have in the cognitiveModels file`, function() {
            it(`an id property with the given name`, function(done) {
                assert.fileContent(
                    join(skillGenerationPath, skillName, cognitiveModelsPath),
                    `"id": "${skillNameCamelCase}",`
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
