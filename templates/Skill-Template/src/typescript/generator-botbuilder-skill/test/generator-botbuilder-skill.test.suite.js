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
    var skillNameId;
    var skillDesc;
    var skillNamePascalCase;
    var skillNameCamelCase;
    var skillNameIdPascalCase;
    var skillUserStateNameClass;
    var skillUserStateNameFile;
    var pathConfirmation;
    var skillGenerationPath;
    var finalConfirmation;
    var run = true;
    var srcFiles;
    var flowTestFiles;
    var skillServices;
    var skillTestServices;
    var packageJSON;
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

    const languageDirectories = [
        "de",
        "en",
        "es",
        "fr",
        "it",
        "zh"
    ];

    const commonTestDirectories = [
        "flow"
    ];
    const commonTestFiles = ["mocha.opts", ".env.test", "testBase.js"];
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
    const mockResourcesFiles = [
        path.join("skills.json"),
        path.join("languageModels.json"),
        path.join("mockedConfiguration.bot")
    ];

    const localeConfigurationTestFiles = [
        "mockedSkillDe.bot",
        "mockedSkillEn.bot",
        "mockedSkillEs.bot",
        "mockedSkillFr.bot",
        "mockedSkillIt.bot",
        "mockedSkillZh.bot"
    ];

    describe("should create", function() {
        skillName = "customSkill";
        skillDesc = "A description for customSkill";
        skillNamePascalCase = _upperFirst(_camelCase(skillName));
        skillNameCamelCase = _camelCase(skillName);
        skillGenerationPath = path.join(__dirname, "tmp");
        skillNameId = skillNameCamelCase.substring(
            0,
            skillNameCamelCase.indexOf("Skill")
        );
        skillNameIdPascalCase = _upperFirst(skillNameId);
        skillUserStateNameClass = `I${skillNamePascalCase.concat(userStateTag)}`;
        skillUserStateNameFile = skillNameCamelCase.concat(
          userStateTag
        );
        pathConfirmation = true;
        finalConfirmation = true;
        srcFiles = ["index.ts", `${skillUserStateNameFile}.ts`, `${skillNameCamelCase}.ts`, "languageModels.json"];
        flowTestFiles = ["interruptionTest.js", "mainDialogTest.js", "sampleDialogTest.js", `${skillName}TestBase.js`];
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
            ]);

            skillServices = require(path.join(skillGenerationPath, skillName, "src", "skills.json"))[0];
            skillTestServices = require(path.join(skillGenerationPath, skillName, "test", "mockResources", "skills.json"))[0];
            packageJSON = require(path.join(skillGenerationPath, skillName, "package.json"));
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
            commonTestFiles.forEach(fileName =>
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "test", fileName)
                    );
                    done();
                })
            );
        });

        describe("in the LocaleConfigurations test folder", function() {
            localeConfigurationTestFiles.forEach(fileName => 
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "test", "mockResources", "LocaleConfigurations", fileName)
                    );
                    done();
                })
            );
        });

        describe("in the flow folder", function() {
            flowTestFiles.forEach(fileName =>
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "test", "flow", fileName)
                    );
                    done();
                }),
            );
        });

        describe("in the mockResources folder", function() {
            mockResourcesFiles.forEach(fileName =>
                it(fileName.concat(" file"), function(done){
                    assert.file(
                        path.join(skillGenerationPath, skillName, "test", "mockResources", fileName)
                    );
                    done();
                }),
            );
        });

        describe("in the mockedSkillDe.bot file", function() {
            it("an id property with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "mockResources", "LocaleConfigurations", "mockedSkillDe.bot"),
                    `"id": "${skillNameId}"`
                );
                done();
            })
        });

        describe("in the mockedSkillEn.bot file", function() {
            it("an id property with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "mockResources", "LocaleConfigurations", "mockedSkillEn.bot"),
                    `"id": "${skillNameId}"`
                );
                done();
            })
        });

        describe("in the mockedSkillEs.bot file", function() {
            it("an id property with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "mockResources", "LocaleConfigurations", "mockedSkillEs.bot"),
                    `"id": "${skillNameId}"`
                );
                done();
            })
        });

        describe("in the mockedSkillFr.bot file", function() {
            it("an id property with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "mockResources", "LocaleConfigurations", "mockedSkillFr.bot"),
                    `"id": "${skillNameId}"`
                );
                done();
            })
        });

        describe("in the mockedSkillIt.bot file", function() {
            it("an id property with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "mockResources", "LocaleConfigurations", "mockedSkillIt.bot"),
                    `"id": "${skillNameId}"`
                );
                done();
            })
        });

        describe("in the mockedSkillZh.bot file", function() {
            it("an id property with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "mockResources", "LocaleConfigurations", "mockedSkillZh.bot"),
                    `"id": "${skillNameId}"`
                );
                done();
            })
        });

        describe("in skills file of the skill", function() {
            it("an id property with the given name", function(done){
                assert.equal(skillServices.id, skillNameCamelCase);
                done();
            }),
            it("a name property with the given name", function(done){
                assert.equal(skillServices.name, skillNameCamelCase);
                done();
            }),
            it("a dispatchIntent property with an intent containing the given name", function(done){
                assert.equal(skillServices.dispatchIntent, `l_${skillNameIdPascalCase}`);
                done();
            }),
            it("a luisServicesIds property with the given name id", function(done){
                assert.equal(skillServices.luisServiceIds[0], skillNameId);
                done();
            }),
            it("a pathToBot property to a path with the given name", function(done){
                assert.equal(skillServices.configuration.pathToBot, `./${skillNameCamelCase}/lib/${skillNameCamelCase}.js`);
                done();
            }),
            it("a ClassName property with a class of a given name", function(done){
                assert.equal(skillServices.configuration.ClassName, skillNamePascalCase);
                done();
            }) 
        });

        describe("in skills file of the tests", function() {
            it("an id property with the given name", function(done){
                assert.equal(skillTestServices.id, skillNameCamelCase);
                done();
            }),
            it("a name property with the given name", function(done){
                assert.equal(skillTestServices.name, skillNameCamelCase);
                done();
            }),
            it("a dispatchIntent property with an intent containing the given name", function(done){
                assert.equal(skillTestServices.dispatchIntent, `l_${skillNameIdPascalCase}`);
                done();
            }),
            it("a luisServicesIds property with the given name id", function(done){
                assert.equal(skillTestServices.luisServiceIds[0], skillNameId);
                done();
            }),
            it("a pathToBot property to a path with the given name", function(done){
                assert.equal(skillTestServices.configuration.pathToBot, `./${skillNameCamelCase}/lib/${skillNameCamelCase}.js`);
                done();
            }),
            it("a ClassName property with a class of a given name", function(done){
                assert.equal(skillTestServices.configuration.ClassName, skillNamePascalCase);
                done();
            }) 
        });
        
        describe("in interruptionTest file", function() {
            it("and have a const value with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "interruptionTest.js"),
                    `const ${skillNameCamelCase} = require('./${skillNameCamelCase}TestBase.js');`
                );
                done();
            }),

            it("and have a call to an object with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "interruptionTest.js"),
                    `${skillNameCamelCase}.initialize();`
                );
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "interruptionTest.js"),
                    `testAdapter = ${skillNameCamelCase}.getTestAdapter();`
                );
                done();
            })  
        });

        describe("in sampleDialogTest file", function() {
            it("and have a const value with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "sampleDialogTest.js"),
                    `const ${skillNameCamelCase} = require('./${skillNameCamelCase}TestBase.js');`
                );
                done();
            }),

            it("and have a call to an object with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "sampleDialogTest.js"),
                    `${skillNameCamelCase}.initialize();`
                );
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "sampleDialogTest.js"),
                    `testAdapter = ${skillNameCamelCase}.getTestAdapter();`
                );
                done();
            })  
        });

        describe("in mainDialogTest file", function() {
            it("and have a const value with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "mainDialogTest.js"),
                    `const ${skillNameCamelCase} = require('./${skillNameCamelCase}TestBase.js');`
                );
                done();
            }),

            it("and have a call to an object with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "mainDialogTest.js"),
                    `${skillNameCamelCase}.initialize();`
                );
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", "mainDialogTest.js"),
                    `testAdapter = ${skillNameCamelCase}.getTestAdapter();`
                );
                done();
            })  
        });

        describe(`in ${skillName}TestBase file`, function() {
            it("and have a const value with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", `${skillName}TestBase.js`),
                    `const ${skillNamePascalCase} = require('../../lib/${skillNameCamelCase}.js').${skillNamePascalCase};`                        
                );
                done();
            }),

            it("and initialize the skill with given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "test", "flow", `${skillName}TestBase.js`),
                    `this.bot = new ${skillNamePascalCase}(services, conversationState, userState, telemetryClient, skillMode);`                        
                );
                done();
            })
        });

        describe("and have in the package.json", function() {
            it("a name property with the given name", function(done) {
                assert.equal(packageJSON.name, skillName);
                done();
            });

            it("a description property with given description", function(done) {
                assert.equal(packageJSON.description, skillDesc);
                done();
            });
        });        

        describe("and have in the mainDialog file", function() {

            it("an import component containing an UserState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `import { ${skillUserStateNameClass} } from '../../${skillUserStateNameFile}';`
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
                    `private projectName: string = '${skillNameId}'`
                );
                done()
            });
        });
             
        describe("and have in the sampleDialog file", function() {
            it("an import component containing an UserState with the given name", function(done) {
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "sample", "sampleDialog.ts"),
                    `import { ${skillUserStateNameClass} } from '../../${skillUserStateNameFile}';`
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

        describe("and have in the LUIS folder", function() {
            languageDirectories.forEach(fileName => 
                it(fileName.concat(" folder with the .lu file"), function(done){
                     assert.file(
                        path.join(skillGenerationPath, skillName, "cognitiveModels", "LUIS", fileName, `${skillNameId}.lu`)
                    );
                    done();
                })
            );
        });

        xdescribe("and have in the skillConversationState file", function() {
            it("a creation of a property containing the skillProjectName with the given name", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "skillConversationState.ts"),
                    `luisResult?: '${skillNameId}LU'`
                );
                done();
            });
        });

        describe("and have in the bot.recipe file", function() {
            languageDirectories.forEach(fileName => 
                it(fileName.concat(" folder with the skillProjectNameId in each bot.recipe file"), function(done){
                    assert.fileContent(
                        path.join(skillGenerationPath, skillName, "deploymentScripts", fileName, "bot.recipe"),
                        `"id": "${skillNameId}"`,
                        `"name": "${skillNameId}"`,
                        `"luPath": "..\\cognitiveModels\\LUIS\\de\\${skillNameId}.lu"`
                    );
                    done();
                })
            );
        });
    
        describe("and have in the skillDialogBase file", function() {

            it("a property with the name of the project", function(done){
                assert.fileContent(
                    path.join(skillGenerationPath, skillName, "src", "dialogs", "main", "mainDialog.ts"),
                    `private projectName: string = '${skillNameId}'`
                );
                done();
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
