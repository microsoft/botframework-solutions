"use strict";
const path = require("path");
const assert = require("yeoman-assert");
const helpers = require("yeoman-test");
const rimraf = require("rimraf");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const semver = require('semver');

describe("The generator-botbuilder-skill tests", function() {
    var skillName;
    var skillDesc;
    var skillNamePascalCase;
    var skillNameCamelCase;
    var skillConversationStateNameClass;
    var skillConversationStateNameFile;
    var skillUserStateNameClass;
    var skillUserStateNameFile;
    var pathConfirmation;
    var skillGenerationPath;
    var finalConfirmation;
    var run = true;
    var srcFiles;
    const conversationStateTag = "ConversationState";
    const userStateTag = "UserState";
    const cognitiveDirectories = ["LUIS"];
    const rootFiles = [
        ".gitignore",
        "tsconfig.json",
        "package.json",
        "tslint.json",
        ".env.development",
        ".env.production"
      ];
    const deploymentFiles = [
        path.join("de", "bot.recipe"),
        path.join("en", "bot.recipe"),
        path.join("es", "bot.recipe"),
        path.join("fr", "bot.recipe"),
        path.join("it", "bot.recipe"),
        path.join("zh", "bot.recipe")
    ]
    const testFiles = [
        "mocha.opts",
        path.join("flow","mainDialogTests.js"),
        path.join("flow","sampleDialogTests.js"),
        path.join("flow","skillTestBase.js")
    ];
    const commonDirectories = [
        "cognitiveModels",
    ];
    
    const commonTestDirectories = [
        "flow"
    ]
    const commonSrcDirectories = [
        path.join("serviceClients"),
        path.join("dialogs"),
    ]
    const commonDialogsDirectories = [
        path.join("main"),
        path.join("main", "resources"),
        path.join("sample"),
        path.join("sample", "resources"),
        path.join("shared"),
        path.join("shared", "dialogOptions"),
        path.join("shared", "resources")
    ]
    const commonDialogsFiles = [
        path.join("main", "mainDialog.ts"),
        path.join("main", "mainResponses.ts"),
        path.join("sample", "sampleDialog.ts"),
        path.join("sample", "sampleResponses.ts"),
        path.join("shared", "skillDialogBase.ts"),
        path.join("shared", "sharedResponses.ts")
    ]

    describe("should create", function() {
        skillName = "mySkill";
        skillDesc = "A description for mySkill";
        skillNamePascalCase = _upperFirst(_camelCase(skillName));
        skillNameCamelCase = _camelCase(skillName);
        skillGenerationPath = path.join(__dirname, "tmp");
        
        skillConversationStateNameClass = `I${skillNamePascalCase.concat(
          conversationStateTag
        )}`;
        skillConversationStateNameFile = skillNameCamelCase.concat(
          conversationStateTag
        );
        skillUserStateNameClass = `I${skillNamePascalCase.concat(userStateTag)}`;
        skillUserStateNameFile = skillNameCamelCase.concat(
          userStateTag
        );
        pathConfirmation = true;
        finalConfirmation = true;
        srcFiles = ["index.ts", `${skillUserStateNameFile}.ts`, `${skillConversationStateNameFile}.ts`, `${skillNameCamelCase}.ts`];
        before(async function(){
            await helpers
            .run(path.join(__dirname, "..", "generators", "app"))
            .inDir(skillGenerationPath)
            .withArguments([
              "-n",
              skillName,
              "-d",
              skillDesc,
              "-p",
              skillGenerationPath,
              "--noPrompt"
            ])
        });

        after(function() {
            rimraf.sync(path.join(__dirname, "tmp", "*"));
        });

        describe("the base", function() {
            it(skillName.concat(" folder"), function(done) {
                assert.file(
                    path.join(skillGenerationPath, skillName)
                );
                done();
            });
        });

        describe("the folders", function() {
            commonDirectories.forEach(directoryName => {
                it(directoryName.concat(" folder"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, directoryName)
                    );
                    done();
                });
            });

            cognitiveDirectories.forEach(directoryName =>
                it(directoryName.concat(" folder"), function(done) {
                    assert.file(
                        path.join(skillGenerationPath, skillName, "cognitiveModels", directoryName)
                    );
                    done();
                })
            );

            commonTestDirectories.forEach(testDirectoryName => 
                it(testDirectoryName.concat(" folder"), function(done) {
                    assert.file(
                      path.join(skillGenerationPath, skillName, "test", testDirectoryName)  
                    );
                    done();
                })
            );
        });

        describe("in the root folder", function() {
            rootFiles.forEach(fileName =>
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, fileName)
                    );
                    done();
                })
            );
        });

        describe("in the deploymentScripts folder", function() {
            deploymentFiles.forEach(fileName =>
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "deploymentScripts", fileName)
                    );
                    done();
                })                
            );
        });

        describe("in the src folder", function() {
            srcFiles.forEach(fileName => 
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "src", fileName)
                    );
                    done();
                })
            );

            commonSrcDirectories.forEach(directoryName => 
                it(directoryName.concat(" folder"), function(done) {
                    assert.file(
                        path.join(skillGenerationPath, skillName, "src", directoryName)
                    )
                    done();
                })
            );
        });

        describe("in the dialogs folder", function() {
            commonDialogsDirectories.forEach(directoryName => 
                it(directoryName.concat(" folder"), function(done) {
                    assert.file(
                        path.join(skillGenerationPath, skillName, "src", "dialogs", directoryName)
                    )
                    done();
                })
            );

            commonDialogsFiles.forEach(fileName => 
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "src", "dialogs", fileName)
                    );
                    done();
                })
            );
        });

        describe("in the test folder", function() {
            testFiles.forEach(fileName =>
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "test", fileName)
                    );
                    done();
                })
            );
        });

        describe("and have in the package.json", function() {
            it("a name property with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "package.json"),
                    `"name": "${skillName}"`
                );
                done();
            });

            it("a description property with given description", function(done) {
                assert.fileContent(
                  path.join(skillGenerationPath, skillName, "package.json"),
                  `"description": "${skillDesc}"`
                );
                done();
            });
        });

        deploymentFiles.forEach(fileName =>
            describe(`and have in the ${fileName} file`, function(done) {
                it("an id key containing a value with the given name", function(done) {
                    assert.fileContent(
                        path.join(skillGenerationPath, skillName, "deploymentScripts", fileName),
                        `"id": "${skillName}"`
                    );
                    done();
                });

                it("a name key containing a value with the given name", function(done) {
                    assert.fileContent(
                        path.join(skillGenerationPath, skillName, "deploymentScripts", fileName),
                        `"name": "${skillName}"`
                    );
                    done();
                });

                it("a luPath key containing a value of the .lu path with the given name", function(done) {
                    assert.fileContent(
                        path.join(skillGenerationPath, skillName, "deploymentScripts", fileName),
                        `"luPath": "..\\\\cognitiveModels\\\\LUIS\\\\${fileName.substring(0,2)}\\\\${skillName}.lu"`
                    );
                    done();
                });
            })
        )
        

        describe("and have in the mainDialog file", function() {
            it("an import component containing a ConversationState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `import { ${skillConversationStateNameClass} } from '../../${skillConversationStateNameFile}';`
                );
                done();
            });

            it("an import component containing an UserState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `import { ${skillUserStateNameClass} } from '../../${skillUserStateNameFile}';`
                );
                done();
            });

            it("an accessor containing a ConversationState with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `StatePropertyAccessor<${skillConversationStateNameClass}>`
                );
                done();
            });

            it("an accessor containing an UserState with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `StatePropertyAccessor<${skillUserStateNameClass}>`
                );
                done();
            });

            it("a creation of a property containing a ConversationState with the given name" , function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `.createProperty('${skillConversationStateNameClass}')`
                );
                done();
            });

            it("a creation of a property containing an UserState with the given name" , function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `.createProperty('${skillUserStateNameClass}')`
                );
                done();
            });

            it("a property with the name of the project", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `private projectName: string = '${skillName}'`
                );
                done()
            });
        });

        describe("and have in the sampleDialog file", function() {
            it("an import component containing a ConversationState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "sample", "sampleDialog.ts"),
                    `import { ${skillConversationStateNameClass} } from '../../${skillConversationStateNameFile}';`
                );
                done();
            });

            it("an import component containing an UserState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "sample", "sampleDialog.ts"),
                    `import { ${skillUserStateNameClass} } from '../../${skillUserStateNameFile}';`
                );
                done();
            });

            it("an accessor containing a ConversationState with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "sample", "sampleDialog.ts"),
                    `StatePropertyAccessor<${skillConversationStateNameClass}>`
                );
                done();
            });

            it("an accessor containing an UserState with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "sample", "sampleDialog.ts"),
                    `StatePropertyAccessor<${skillUserStateNameClass}>`
                );
                done();
            });
        });

        describe("and have in the skillDialogBase file", function() {
            it("an import component containing a ConversationState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "shared", "skillDialogBase.ts"),
                    `import { ${skillConversationStateNameClass} } from '../../${skillConversationStateNameFile}';`
                );
                done();
            });

            it("an import component containing an UserState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "shared", "skillDialogBase.ts"),
                    `import { ${skillUserStateNameClass} } from '../../${skillUserStateNameFile}';`
                );
                done();
            });

            it("an accessor containing a ConversationState with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "shared", "skillDialogBase.ts"),
                    `StatePropertyAccessor<${skillConversationStateNameClass}>`
                );
                done();
            });

            it("an accessor containing an UserState with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "shared", "skillDialogBase.ts"),
                    `StatePropertyAccessor<${skillUserStateNameClass}>`
                );
                done();
            });

            it("a property with the name of the project", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `private projectName: string = '${skillName}'`
                );
                done()
            });
        });
    });

   describe("should not create", function() {
        before(async function() {
            if(semver.gte(process.versions.node, '10.12.0')){
                run = false;
            }
            else {
                finalConfirmation = false;
                await helpers
                    .run(path.join(
                        __dirname,
                        "..",
                        "generators",
                        "app"
                    ))
                    .inDir(skillGenerationPath)
                    .withPrompts({
                        skillName: skillName,
                        skillDesc: skillDesc,
                        pathConfirmation: pathConfirmation,
                        skillGenerationPath: skillGenerationPath,
                        finalConfirmation: finalConfirmation
                    });
            }
        });

        after(function() {
            rimraf.sync(path.join(__dirname, "tmp", "*"));
        });

        describe("the base", function() {
            it(skillName + " folder when the final confirmation is deny", function(done) {
              if(!run){
                this.skip()          
              }
              assert.noFile(skillGenerationPath, skillName);
              done();
            });
        });
    })
});
