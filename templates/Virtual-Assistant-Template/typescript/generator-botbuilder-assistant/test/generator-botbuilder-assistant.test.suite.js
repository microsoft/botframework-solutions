/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
"use strict";
const path = require(`path`);
const assert = require(`yeoman-assert`);
const helpers = require(`yeoman-test`);
const rimraf = require(`rimraf`);
const _kebabCase = require(`lodash/kebabCase`);
const semver = require('semver');
const languages = [`zh`, `de`, `en`, `fr`, `it`, `es`];

describe(`The generator-botbuilder-assistant tests`, function() {
    var assistantName;
    var assistantDesc;
    var pathConfirmation;
    var assistantGenerationPath;
    var finalConfirmation;
    var run = true;
    var packageJSON;

    const templatesFiles = [
        `package.json`,
        `.gitignore`
    ];
    const commonDirectories = [
        `deployment`,
        path.join(`deployment`, `resources`),
        path.join("deployment", "scripts"),
        `src`,
        path.join(`src`, `adapters`),
        path.join(`src`, `bots`),
        path.join(`src`, `content`),
        path.join(`src`, `dialogs`),
        path.join(`src`, `locales`),
        path.join(`src`, `models`),
        path.join(`src`, `responses`),
        path.join(`src`, `services`),
        `test`,
        path.join(`test`, `flow`),
    ];

    describe(`should create`, function() {
        assistantName = `customAssistant`;
        assistantDesc = `A description for customAssistant`;
        assistantName = _kebabCase(assistantName).replace(
            /([^a-z0-9-]+)/gi,
            ``
        ); 
        assistantGenerationPath = path.join(__dirname, "tmp");
        pathConfirmation = true;
        finalConfirmation = true;

        before(async function(){
            await helpers
            .run(path.join(__dirname, `..`, `generators`, `app`))
            .inDir(assistantGenerationPath)
            .withArguments([
              `-n`,
              assistantName,
              `-d`,
              assistantDesc,
              `-p`,
              assistantGenerationPath,
              `--noPrompt`
            ]);

            packageJSON = require(path.join(assistantGenerationPath, assistantName, `package.json`));
        });

        after(function() {
            rimraf.sync(path.join(__dirname, `tmp`, `*`));
        });

        describe(`the base`, function() {
            it(assistantName.concat(` folder`), function(done) {
                assert.file(
                    path.join(assistantGenerationPath, assistantName)
                );
                done();
            });
        });

        describe(`the folders`, function() {
            commonDirectories.forEach(directoryName => 
                it(directoryName.concat(" folder"), function(done) {
                    assert.file(
                        path.join(assistantGenerationPath, assistantName, directoryName)
                    )
                    done();
                })
            );
        });

        describe(`the files`, function() {
            templatesFiles.forEach(templateFile => 
                it(templateFile.concat(" file"), function(done) {
                    assert.file(
                        path.join(assistantGenerationPath, assistantName, templateFile)
                    )
                    done();
                })
            );
        });

        describe(`and have in the package.json`, function() {
            it("a name property with the given name", function(done) {
                assert.equal(packageJSON.name, assistantName);
                done();
            });

            it("a description property with given description", function(done) {
                assert.equal(packageJSON.description, assistantDesc);
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
                    .run(path.join(
                        __dirname,
                        `..`,
                        `generators`,
                        `app`
                    ))
                    .inDir(assistantGenerationPath)
                    .withPrompts({
                        assistantName: assistantName,
                        assistantDesc: assistantDesc,
                        assistantLang: languages,
                        pathConfirmation: pathConfirmation,
                        assistantGenerationPath: assistantGenerationPath,
                        finalConfirmation: finalConfirmation
                    });
            }
        });

        after(function() {
            rimraf.sync(path.join(__dirname, `tmp`, `*`));
        });

        describe(`the base`, function() {
            it(assistantName + ` folder when the final confirmation is deny`, function(done) {
              if(!run){
                this.skip()          
              }
              assert.noFile(assistantGenerationPath, assistantName);
              done();
            });
        });
    });
});
