/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
"use strict";
const join = require(`path`).join;
const assert = require(`yeoman-assert`);
const helpers = require(`yeoman-test`);
const rimraf = require(`rimraf`);
const _kebabCase = require(`lodash/kebabCase`);
const _camelCase = require(`lodash/camelCase`);
const semver = require('semver');
const someLanguages = [`zh-cn`, `de-de`, `en-us`];
const sinon = require(`sinon`);

describe(`The generator-bot-virtualassistant tests`, function() {
    var assistantName;
    var assistantDesc;
    var assistantNameCamelCase;
    var pathConfirmation;
    var assistantGenerationPath;
    var finalConfirmation;
    var run = true;
    var packageJSON;
    const botPath = join(`src`, `bots`, `defaultActivityHandler.ts`);

    const templatesFiles = [
        `package.json`,
        `.eslintrc.json`,
        `.eslintignore`,
        `.gitignore`,
        `.nycrc`,
        botPath
    ];
    const commonDirectories = [
        `deployment`,
        join(`deployment`, `resources`),
        join("deployment", "scripts"),
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
        assistantName = `sample-assistant`;
        assistantDesc = `A description for sample-assistant`;
        assistantName = _kebabCase(assistantName).replace(
            /([^a-z0-9-]+)/gi,
            ``
        ); 
        assistantNameCamelCase = _camelCase(assistantName).replace(
            /([^a-z0-9-]+)/gi,
            ``
        ); 
        assistantGenerationPath = join(__dirname, "tmp");
        pathConfirmation = true;
        finalConfirmation = true;
        const pipelinePath = join(`pipeline`, `${assistantName}.yml`);
        templatesFiles.push(pipelinePath);

        before(async function(){
            await helpers
            .run(join(__dirname, `..`, `generators`, `app`))
            .inDir(assistantGenerationPath)
            .withArguments([
                `-n`,
                assistantName,
                `-d`,
                assistantDesc,
                `-l`,
                someLanguages.join(`,`),
                `-p`,
                assistantGenerationPath,
                `--noPrompt`
            ])
            .on('ready', generator => {
                generator.spawnCommandSync = sinon.spy();
            });

            packageJSON = require(join(assistantGenerationPath, assistantName, `package.json`));
        });

        after(function() {
            rimraf(join(__dirname, `tmp`, `**`), function () {});
            process.chdir(join(__dirname, `..`));
        });

        describe(`the base`, function() {
            it(assistantName.concat(` folder`), function(done) {
                assert.file(
                    join(assistantGenerationPath, assistantName)
                );
                done();
            });
        });

        describe(`the folders`, function() {
            commonDirectories.forEach(directoryName => 
                it(directoryName.concat(" folder"), function(done) {
                    assert.file(
                        join(assistantGenerationPath, assistantName, directoryName)
                    )
                    done();
                })
            );
        });

        describe(`the files`, function() {
            templatesFiles.forEach(templateFile => 
                it(templateFile.concat(" file"), function(done) {
                    assert.file(
                        join(assistantGenerationPath, assistantName, templateFile)
                    )
                    done();
                })
            );
        });

        describe(`and have in the package.json`, function() {
            it("a name property with the given name", function(done) {
                assert.strictEqual(packageJSON.name, assistantName);
                done();
            });

            it("a description property with given description", function(done) {
                assert.strictEqual(packageJSON.description, assistantDesc);
                done();
            });
        });
    });
    
    describe(`should not create`, function () {
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
                        `app`
                    ))
                    .inDir(assistantGenerationPath)
                    .withPrompts({
                        assistantName: assistantName,
                        assistantDesc: assistantDesc,
                        assistantLang: someLanguages,
                        pathConfirmation: pathConfirmation,
                        assistantGenerationPath: assistantGenerationPath,
                        finalConfirmation: finalConfirmation
                    })
                    .on('ready', generator => {
                        generator.spawnCommandSync = sinon.spy();
                    });
            }
        });

        after(function() {
            rimraf(join(__dirname, `tmp`, `**`), function () {});
            process.chdir(join(__dirname, `..`));
        });

        describe(`the base`, function() {
            it(assistantName + ` folder when the execution is skipped`, function(done) {
              if(!run){
                this.skip()          
              }
              assert.noFile(assistantGenerationPath, assistantName);
              done();
            });
        });
    });
});
