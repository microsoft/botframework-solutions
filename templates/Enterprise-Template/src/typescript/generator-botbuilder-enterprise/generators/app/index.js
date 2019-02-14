"use strict";
const Generator = require("yeoman-generator");
const chalk = require("chalk");
const path = require("path");
const pkg = require("../../package.json");
const _pick = require("lodash/pick");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const _kebabCase = require("lodash/kebabCase");
const fs = require("fs");

const languages = [
  {
    name: "German",
    value: "de"
  },
  {
    name: "English",
    value: "en"
  },
  {
    name: "Spanish",
    value: "es"
  },
  {
    name: "French",
    value: "fr"
  },
  {
    name: "Italian",
    value: "it"
  },
  {
    name: "Chinese",
    value: "zh"
  }
];

const bigBot =
  "               ╭─────────────────────────╮\n" +
  "   " +
  chalk.blue.bold("//") +
  "     " +
  chalk.blue.bold("\\\\") +
  "   │     Welcome to the      │\n" +
  "  " +
  chalk.blue.bold("//") +
  " () () " +
  chalk.blue.bold("\\\\") +
  "  │  BotBuilder Enterprise  │\n" +
  "  " +
  chalk.blue.bold("\\\\") +
  "       " +
  chalk.blue.bold("//") +
  " /│        generator!       │\n" +
  "   " +
  chalk.blue.bold("\\\\") +
  "     " +
  chalk.blue.bold("//") +
  "   ╰─────────────────────────╯\n" +
  `                                    v${pkg.version}`;

const tinyBot =
  " " + chalk.blue.bold("<") + " ** " + chalk.blue.bold(">") + " ";

var botGenerationPath = process.cwd();
var isAlreadyCreated = false;

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option("botName", {
      description: "The name you want to give to your bot.",
      type: String,
      default: "enterprise-bot",
      alias: "n"
    });

    this.option("botDesc", {
      description: "A brief bit of text used to describe what your bot does.",
      type: String,
      alias: "d"
    });

    this.option("botLang", {
      description: "The language you want to use with your bot.",
      type: String,
      default: "en",
      alias: "l"
    });

    this.option("botGenerationPath", {
      description: "The path where the bot will be generated.",
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
    this.log(
      chalk.magenta(
        "\nREMINDER: For deployment's steps, the bot's name needs to be unique."
      )
    );
    // Validate language option
    if (
      this.options.botLang &&
      !languages.some(l => l.value === this.options.botLang)
    ) {
      this.log.error(
        "ERROR: Language must be selected from the list:\n\t" +
          languages.map(l => `${l.value} -> ${l.name}`).join("\n\t") +
          "\nDefault value: en"
      );
      process.exit(1);
    }

    const selectedLang = languages.findIndex(
      l => l.value === this.options.botLang
    );

    const prompts = [
      {
        type: "input",
        name: "botName",
        message: `What's the name of your bot?`,
        default: this.options.botName ? this.options.botName : "enterprise-bot"
      },
      {
        type: "input",
        name: "botDesc",
        message: "What will your bot do?",
        default: this.options.botDesc
          ? this.options.botDesc
          : "Demonstrate advanced capabilities of a Conversational AI bot."
      },
      {
        type: "list",
        name: "botLang",
        message: "What language will your bot use?",
        choices: languages,
        default: selectedLang
      },
      {
        type: "confirm",
        name: "pathConfirmation",
        message: "Do you want to change the location of the generation?",
        default: false
      },
      {
        when: function(response) {
          return response.pathConfirmation === true;
        },
        type: "input",
        name: "botGenerationPath",
        message: "Where do you want to generate the bot?",
        default: this.options.botGenerationPath
          ? this.options.botGenerationPath
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
      {
        type: "confirm",
        name: "finalConfirmation",
        message: "Looking good. Shall I go ahead and create your new bot?",
        default: true
      }
    ];

    if (this.options.noPrompt) {
      this.props = _pick(this.options, [
        "botName",
        "botDesc",
        "botLang",
        "botGenerationPath"
      ]);

      // Validate we have what we need, or we'll need to throw
      if (!this.props.botName) {
        this.log.error(
          "ERROR: Must specify a name for your bot when using --noPrompt argument. Use --botName or -n option."
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

    const templateName = "enterprise-bot";
    const botLang = this.props.botLang;
    const botDesc = this.props.botDesc;
    if (!this.props.botName.replace(/\s/g, "").length) {
      this.props.botName = templateName;
    }

    const botName = _kebabCase(this.props.botName).replace(
      /([^a-z0-9-]+)/gi,
      ""
    );
    const botNamePascalCase = _upperFirst(_camelCase(this.props.botName));
    const botNameCamelCase = _camelCase(this.props.botName);
    botGenerationPath = path.join(botGenerationPath, botName);
    if (this.props.botGenerationPath !== undefined) {
      botGenerationPath = path.join(this.props.botGenerationPath, botName);
    }

    if (fs.existsSync(botGenerationPath)) {
      isAlreadyCreated = true;
      return;
    }

    this.log(chalk.magenta("\nCurrent values for the new bot:"));
    this.log(chalk.magenta("Name: " + botName));
    this.log(chalk.magenta("Description: " + botDesc));
    this.log(chalk.magenta("Language: " + botLang));
    this.log(chalk.magenta("Path: " + botGenerationPath + "\n"));

    this.fs.copy(
      this.templatePath(templateName, "cognitiveModels", "LUIS", botLang, "*"),
      this.destinationPath(
        botGenerationPath,
        "cognitiveModels",
        "LUIS",
        botLang
      )
    );

    this.fs.copy(
      this.templatePath(templateName, "cognitiveModels", "QnA", botLang, "*"),
      this.destinationPath(botGenerationPath, "cognitiveModels", "QnA", botLang)
    );

    this.fs.copy(
      this.templatePath(templateName, "deploymentScripts", botLang, "*"),
      this.destinationPath(botGenerationPath, "deploymentScripts", botLang)
    );

    this.fs.copy(
      this.templatePath(templateName, "src", "locales"),
      this.destinationPath(botGenerationPath, "src", "locales")
    );

    this.fs.copyTpl(
      this.templatePath(templateName, "_package.json"),
      this.destinationPath(botGenerationPath, "package.json"),
      {
        name: botName,
        description: botDesc
      }
    );

    this.fs.copyTpl(
      this.templatePath(templateName, "src", "_index.ts"),
      this.destinationPath(botGenerationPath, "src", "index.ts"),
      {
        name: botName,
        description: botDesc,
        botNameClass: botNamePascalCase,
        botNameFile: botNameCamelCase
      }
    );

    this.fs.copyTpl(
      this.templatePath(templateName, "src", "_enterpriseBot.ts"),
      this.destinationPath(botGenerationPath, "src", `${botNameCamelCase}.ts`),
      {
        name: botName,
        description: botDesc,
        botNameClass: botNamePascalCase,
        botNameFile: botNameCamelCase
      }
    );

    this.fs.copyTpl(
      this.templatePath(templateName, "test", "flow", "_botTestBase.js"),
      this.destinationPath(botGenerationPath, "test", "flow", "botTestBase.js"),
      {
        name: botName,
        description: botDesc,
        botNameClass: botNamePascalCase,
        botNameFile: botNameCamelCase
      }
    );

    this.fs.copy(
      this.templatePath(templateName, "src", "botServices.ts"),
      this.destinationPath(botGenerationPath, "src", "botServices.ts")
    );

    this.fs.copy(
      this.templatePath(templateName, "_.gitignore"),
      this.destinationPath(botGenerationPath, ".gitignore")
    );

    const commonFiles = [
      ".env.development",
      ".env.production",
      "README.md",
      "tsconfig.json",
      "deploymentScripts/webConfigPrep.js",
      "tslint.json"
    ];

    commonFiles.forEach(fileName =>
      this.fs.copy(
        this.templatePath(templateName, fileName),
        this.destinationPath(botGenerationPath, fileName)
      )
    );

    const commonDirectories = [
      "dialogs",
      "extensions",
      "middleware",
      "serviceClients"
    ];

    commonDirectories.forEach(directory =>
      this.fs.copy(
        this.templatePath(templateName, "src", directory, "**", "*"),
        this.destinationPath(botGenerationPath, "src", directory)
      )
    );

    const commonTestFlowFiles = [
      "escalateDialogTest.js",
      "mainDialogTest.js",
      "onboardingDialogTest.js"
    ];

    commonTestFlowFiles.forEach(testFlowFileName =>
      this.fs.copy(
        this.templatePath(templateName, "test", "flow", testFlowFileName),
        this.destinationPath(
          botGenerationPath,
          "test",
          "flow",
          testFlowFileName
        )
      )
    );

    const commonTestFiles = [
      ".env.test",
      "mocha.opts",
      "mockedConfiguration.bot",
      "testBase.js"
    ];

    commonTestFiles.forEach(testFileName =>
      this.fs.copy(
        this.templatePath(templateName, "test", testFileName),
        this.destinationPath(botGenerationPath, "test", testFileName)
      )
    );

    const commonTestDirectories = ["nockFixtures"];

    commonTestDirectories.forEach(directory =>
      this.fs.copy(
        this.templatePath(templateName, "test", directory, "**", "*"),
        this.destinationPath(botGenerationPath, "test", directory)
      )
    );
  }

  install() {
    if (this.props.finalConfirmation !== true || isAlreadyCreated) {
      return;
    }

    process.chdir(botGenerationPath);
    this.installDependencies({ npm: true, bower: false });
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
            " ERROR: It's seems like you already have a bot with the same name in the destination path. "
          )
        );
        this.log(
          chalk.red.bold(
            " Try again changing the name or the destination path or deleting the previous bot. "
          )
        );
        this.log(
          chalk.red.bold(
            "-------------------------------------------------------------------------------------------- "
          )
        );
      } else {
        this.log(chalk.green("------------------------ "));
        this.log(chalk.green(" Your new bot is ready!  "));
        this.log(chalk.green("------------------------ "));
        this.log(
          "Open the " +
            chalk.green.bold("README.md") +
            " to learn how to run your bot. "
        );
      }
    } else {
      this.log(chalk.red.bold("-------------------------------- "));
      this.log(chalk.red.bold(" New bot creation was canceled. "));
      this.log(chalk.red.bold("-------------------------------- "));
    }

    this.log("Thank you for using the Microsoft Bot Framework. ");
    this.log("\n" + tinyBot + "The Bot Framework Team");
  }
};
