// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

"use strict";
const Generator = require("yeoman-generator");
const chalk = require("chalk");
const path = require("path");
const pkg = require("../../package.json");
const _pick = require("lodash/pick");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const fs = require("fs");

const bigBot =
  "               ╭────────────────────────╮\n" +
  "   " +
  chalk.blue.bold("//") +
  "     " +
  chalk.blue.bold("\\\\") +
  "   │     Welcome to the     │\n" +
  "  " +
  chalk.blue.bold("//") +
  " () () " +
  chalk.blue.bold("\\\\") +
  "  │    BotBuilder Skill    │\n" +
  "  " +
  chalk.blue.bold("\\\\") +
  "       " +
  chalk.blue.bold("//") +
  " /│       generator!       │\n" +
  "   " +
  chalk.blue.bold("\\\\") +
  "     " +
  chalk.blue.bold("//") +
  "   ╰────────────────────────╯\n" +
  `                                    v${pkg.version}`;

const tinyBot =
  " " + chalk.blue.bold("<") + " ** " + chalk.blue.bold(">") + " ";

var skillGenerationPath = process.cwd();
var isAlreadyCreated = false;

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option("skillName", {
      description: "The name you want to give to your skill.",
      type: String,
      default: "customSkill",
      alias: "n"
    });

    this.option("skillDesc", {
      description: "A brief bit of text used to describe what your skill does.",
      type: String,
      alias: "d"
    });

    this.option("skillGenerationPath", {
      description: "The path where the skill will be generated.",
      type: String,
      default: process.cwd(),
      alias: "p"
    });

    this.option("noPrompt", {
      description: "Do not prompt for any information or confirmation.",
      type: Boolean,
      default: false
    });
  }

  prompting() {
    this.log(bigBot);
    // Generate the main prompts
    const prompts = [
      // Name of the skill
      {
        type: "input",
        name: "skillName",
        message: "What's the name of your skill?",
        default: this.options.skillName ? this.options.skillName : "customSkill"
      },
      // Description of the skill
      {
        type: "input",
        name: "skillDesc",
        message: "What will your skill do?",
        default: this.options.skillDesc ? this.options.skillDesc : ""
      },
      // Path of the skill
      {
        type: "confirm",
        name: "pathConfirmation",
        message: "Do you want to change the new skill's location?",
        default: false
      },
      {
        when: function(response) {
          return response.pathConfirmation === true;
        },
        type: "input",
        name: "skillGenerationPath",
        message: "Where do you want to generate the skill?",
        default: this.options.skillGenerationPath
          ? this.options.skillGenerationPath
          : process.cwd(),
        validate: path => {
          if (fs.existsSync(path)) {
            return true;
          }

          this.log(
            chalk.red(
              "\n",
              "ERROR: This is not a valid path. Please try again."
            )
          );
        }
      },
      // Final confirmation of the skill
      {
        type: "confirm",
        name: "finalConfirmation",
        message: "Looking good. Shall I go ahead and create your new skill?",
        default: true
      }
    ];

    // Check that if it was generated with CLI commands
    if (this.options.noPrompt) {
      this.props = _pick(this.options, [
        "skillName",
        "skillDesc",
        "skillGenerationPath"
      ]);

      // Validate we have what we need, or we'll need to throw
      if (!this.props.skillName) {
        this.log.error(
          "ERROR: Must specify a name for your skill when using --noPrompt argument. Use --skillName or -n option."
        );
        process.exit(1);
      }

      this.props.finalConfirmation = true;
      return;
    }

    return this.prompt(prompts).then(props => {
      this.props = props;
    });
  }

  writing() {
    if (this.props.finalConfirmation !== true) {
      return;
    }

    const templateName = "customSkill";
    const userStateTag = "UserState";
    const skillDesc = this.props.skillDesc;
    if (!this.props.skillName.replace(/\s/g, "").length) {
      this.props.skillName = templateName;
    }

    const skillName = this.props.skillName;

    // Generate vars for templates
    const skillNamePascalCase = _upperFirst(_camelCase(this.props.skillName));
    const skillNameCamelCase = _camelCase(this.props.skillName);
    const skillNameId = skillNameCamelCase.substring(
      0,
      skillNameCamelCase.indexOf("Skill")
    );
    skillGenerationPath = path.join(skillGenerationPath, skillName);
    if (this.props.skillGenerationPath !== undefined) {
      skillGenerationPath = path.join(
        this.props.skillGenerationPath,
        skillName
      );
    }

    const skillUserStateNameClass = `I${skillNamePascalCase.concat(
      userStateTag
    )}`;
    const skillUserStateNameFile = skillNameCamelCase.concat(userStateTag);

    if (fs.existsSync(skillGenerationPath)) {
      isAlreadyCreated = true;
      return;
    }

    // Print current values
    this.log(chalk.magenta("\nCurrent values for the new skill:"));
    this.log(chalk.magenta("Name: " + skillName));
    this.log(chalk.magenta("Description: " + skillDesc));
    this.log(chalk.magenta("Path: " + skillGenerationPath + "\n"));

    // Copy package.json
    this.fs.copyTpl(
      this.templatePath(templateName, "_package.json"),
      this.destinationPath(skillGenerationPath, "package.json"),
      {
        name: skillName,
        description: skillDesc
      }
    );

    // Copy bot.recipes files
    const recipeFiles = [
      path.join("de", "bot.recipe"),
      path.join("en", "bot.recipe"),
      path.join("es", "bot.recipe"),
      path.join("fr", "bot.recipe"),
      path.join("it", "bot.recipe"),
      path.join("zh", "bot.recipe")
    ];

    recipeFiles.forEach(fileName =>
      this.fs.copyTpl(
        this.templatePath(templateName, "deploymentScripts", fileName),
        this.destinationPath(
          skillGenerationPath,
          "deploymentScripts",
          fileName
        ),
        {
          name: skillName
        }
      )
    );

    // Copy index.ts
    this.fs.copyTpl(
      this.templatePath(templateName, "src", "_index.ts"),
      this.destinationPath(skillGenerationPath, "src", "index.ts"),
      {
        skillUserStateNameClass: skillUserStateNameClass,
        skillUserStateNameFile: skillUserStateNameFile,
        skillProjectName: skillName,
        skillProjectNameLU: `${skillName}LU`
      }
    );

    // Copy skillConversationState.ts
    this.fs.copyTpl(
      this.templatePath(templateName, "src", "_skillConversationState.ts"),
      this.destinationPath(
        skillGenerationPath,
        "src",
        "skillConversationState.ts"
      ),
      {
        skillProjectNameLU: `${skillNameId}LU`
      }
    );

    // Copy skillUserState.ts
    this.fs.copyTpl(
      this.templatePath(templateName, "src", "_skillUserState.ts"),
      this.destinationPath(
        skillGenerationPath,
        "src",
        `${skillUserStateNameFile}.ts`
      ),
      {
        skillUserStateNameClass: skillUserStateNameClass,
        skillUserStateNameFile: skillUserStateNameFile,
        skillProjectName: skillName,
        skillProjectNameLU: `${skillName}LU`
      }
    );

    // Copy skillTemplate.ts
    this.fs.copyTpl(
      this.templatePath(templateName, "src", "_skillTemplate.ts"),
      this.destinationPath(
        skillGenerationPath,
        "src",
        `${skillNameCamelCase}.ts`
      ),
      {
        skillUserStateNameClass: skillUserStateNameClass,
        skillUserStateNameFile: skillUserStateNameFile,
        skillTemplateName: skillNamePascalCase,
        skillProjectName: skillName,
        skillProjectNameLU: `${skillName}LU`
      }
    );

    const languageDirectories = ["de", "en", "es", "fr", "it", "zh"];

    languageDirectories.forEach(language =>
      this.fs.copy(
        this.templatePath(
          templateName,
          "cognitiveModels",
          "LUIS",
          language,
          "_skill.lu"
        ),
        this.destinationPath(
          skillGenerationPath,
          "cognitiveModels",
          "LUIS",
          language,
          `${skillNameId}.lu`
        )
      )
    );

    // Copy bot.recipes files
    languageDirectories.forEach(languageDirectory =>
      this.fs.copyTpl(
        this.templatePath(
          templateName,
          "deploymentScripts",
          languageDirectory,
          "_bot.recipe"
        ),
        this.destinationPath(
          skillGenerationPath,
          "deploymentScripts",
          languageDirectory,
          "bot.recipe"
        ),
        {
          skillProjectName: skillNameId
        }
      )
    );

    // Copy mainDialog.ts
    this.fs.copyTpl(
      this.templatePath(
        templateName,
        "src",
        "dialogs",
        "main",
        "_mainDialog.ts"
      ),
      this.destinationPath(
        skillGenerationPath,
        "src",
        "dialogs",
        "main",
        "mainDialog.ts"
      ),
      {
        skillUserStateNameClass: skillUserStateNameClass,
        skillUserStateNameFile: skillUserStateNameFile,
        skillProjectName: skillName
      }
    );

    // Copy sampleDialog.ts
    this.fs.copyTpl(
      this.templatePath(
        templateName,
        "src",
        "dialogs",
        "sample",
        "_sampleDialog.ts"
      ),
      this.destinationPath(
        skillGenerationPath,
        "src",
        "dialogs",
        "sample",
        "sampleDialog.ts"
      ),
      {
        skillUserStateNameClass: skillUserStateNameClass,
        skillUserStateNameFile: skillUserStateNameFile
      }
    );

    // Copy skillDialogBase.ts
    this.fs.copyTpl(
      this.templatePath(
        templateName,
        "src",
        "dialogs",
        "shared",
        "_skillDialogBase.ts"
      ),
      this.destinationPath(
        skillGenerationPath,
        "src",
        "dialogs",
        "shared",
        "skillDialogBase.ts"
      ),
      {
        skillUserStateNameClass: skillUserStateNameClass,
        skillUserStateNameFile: skillUserStateNameFile,
        skillProjectName: skillName
      }
    );

    // Copy gitignore
    this.fs.copy(
      this.templatePath(templateName, "_.gitignore"),
      this.destinationPath(skillGenerationPath, ".gitignore")
    );

    // Copy commonFiles
    const commonFiles = [
      "tsconfig.json",
      "tslint.json",
      ".env.development",
      ".env.production",
      path.join("deploymentScripts", "bot.recipe"),
      path.join("deploymentScripts", "deploy_bot.ps1"),
      path.join("deploymentScripts", "generate_deployment_scripts.ps1"),
      path.join("deploymentScripts", "update_published_models.ps1"),
      path.join("src", "dialogs", "main", "mainResponses.ts"),
      path.join("src", "dialogs", "sample", "sampleResponses.ts"),
      path.join("src", "dialogs", "shared", "sharedResponses.ts")
    ];

    commonFiles.forEach(fileName =>
      this.fs.copy(
        this.templatePath(templateName, fileName),
        this.destinationPath(skillGenerationPath, fileName)
      )
    );

    // Copy commonDirectories
    const commonDirectories = [
      path.join("src", "serviceClients"),
      path.join("src", "dialogs", "main", "resources"),
      path.join("src", "dialogs", "sample", "resources"),
      path.join("src", "dialogs", "shared", "resources"),
      path.join("src", "dialogs", "shared", "dialogOptions")
    ];

    commonDirectories.forEach(directory =>
      this.fs.copy(
        this.templatePath(templateName, directory, "**", "*"),
        this.destinationPath(skillGenerationPath, directory)
      )
    );

    // Copy commonFlowFiles
    const commonTestFiles = [
      path.join("flow", "mainDialogTests.js"),
      path.join("flow", "sampleDialogTests.js"),
      path.join("flow", "skillTestBase.js"),
      "mocha.opts"
    ];

    commonTestFiles.forEach(testFlowFileName =>
      this.fs.copy(
        this.templatePath(templateName, "test", testFlowFileName),
        this.destinationPath(skillGenerationPath, "test", testFlowFileName)
      )
    );
  }

  end() {
    if (this.props.finalConfirmation === true) {
      if (isAlreadyCreated) {
        this.log(
          chalk.red.bold(
            "-------------------------------------------------------------------------------------------- "
          )
        );
        this.log(
          chalk.red.bold(
            " ERROR: It's seems like you already have a skill with the same name in the destination path. "
          )
        );
        this.log(
          chalk.red.bold(
            " Try again changing the name or the destination path or deleting the previous skill. "
          )
        );
        this.log(
          chalk.red.bold(
            "-------------------------------------------------------------------------------------------- "
          )
        );
      } else {
        this.log(
          chalk.green(
            "---------------------------------------------------------- "
          )
        );
        this.log(chalk.green(" Your new skill is ready!  "));
        this.log(
          chalk.green(
            " Now you are able to install the dependencies and build it!"
          )
        );
        this.log(
          chalk.green(
            "---------------------------------------------------------- "
          )
        );
        this.log(
          "Open the " +
            chalk.green.bold("README.md") +
            " to learn how to run your skill. "
        );
      }
    } else {
      this.log(chalk.red.bold("-------------------------------- "));
      this.log(chalk.red.bold(" New skill creation was canceled. "));
      this.log(chalk.red.bold("-------------------------------- "));
    }

    this.log("Thank you for using the Microsoft Bot Framework. ");
    this.log("\n" + tinyBot + "The Bot Framework Team");
  }
};
