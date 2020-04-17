/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

"use strict";
const { Copier } = require("./src/copier");
const Generator = require(`yeoman-generator`);
const chalk = require(`chalk`);
const fs = require("fs");
const path = require(`path`);
const pkg = require(`../../package.json`);
const _pick = require(`lodash/pick`);
const _kebabCase = require(`lodash/kebabCase`);
const _camelCase = require(`lodash/camelCase`);
const templateName = "sample-assistant";
const languages = [`zh-cn`, `de-de`, `en-us`, `fr-fr`, `it-it`, `es-es`];
let assistantGenerationPath = process.cwd();
let isAlreadyCreated = false;
let containsSpecialCharacter = false;
let finalAssistantName = "";

const languagesChoice = [
  {
    name: "Chinese: zh-cn",
    value: "zh-cn",
    checked: true
  },
  {
    name: "Deutsch: de-de",
    value: "de-de",
    checked: true
  },
  {
    name: "English: en-us",
    value: "en-us",
    checked: true
  },
  {
    name: "French: fr-fr",
    value: "fr-fr",
    checked: true
  },
  {
    name: "Italian: it-it",
    value: "it-it",
    checked: true
  },
  {
    name: "Spanish: es-es",
    value: "es-es",
    checked: true
  }
];

const bigBot =
  `               ╭─────────────────────────────────╮\n` +
  `   ` +
  chalk.blue.bold(`//`) +
  `     ` +
  chalk.blue.bold(`\\\\`) +
  `   │        Welcome to the           │\n` +
  `  ` +
  chalk.blue.bold(`//`) +
  ` () () ` +
  chalk.blue.bold(`\\\\`) +
  `  │      Bot Virtual Assistant      │\n` +
  `  ` +
  chalk.blue.bold(`\\\\`) +
  `       ` +
  chalk.blue.bold(`//`) +
  ` /│          generator!             │\n` +
  `   ` +
  chalk.blue.bold(`\\\\`) +
  `     ` +
  chalk.blue.bold(`//`) +
  `   ╰─────────────────────────────────╯\n` +
  `                                    v${pkg.version}`;

const tinyBot =
  ` ` + chalk.blue.bold(`<`) + ` ** ` + chalk.blue.bold(`>`) + ` `;

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option(`assistantName`, {
      description: `The name you want to give to your assistant.`,
      type: String,
      default: `sample-assistant`,
      alias: `n`
    });

    this.option(`assistantDesc`, {
      description: `A brief bit of text used to describe what your assistant does.`,
      type: String,
      alias: `d`
    });

    this.option(`assistantLang`, {
      description: `The languages you want to use with your assistant.`,
      type: String,
      alias: `l`,
      default: languages.join()
    });

    this.option(`assistantGenerationPath`, {
      description: `The path where the assistant will be generated.`,
      type: String,
      default: process.cwd(),
      alias: `p`
    });

    this.option(`noPrompt`, {
      description: `Do not prompt for any information or confirmation.`,
      type: Boolean,
      default: false
    });

    // Instantiate the copier
    this.copier = new Copier(this);
  }

  prompting() {
    this.log(bigBot);
    // Validate language option
    if (this.options.assistantLang) {
      this.options.assistantLang = this.options.assistantLang
        .replace(/\s/g, "")
        .split(",");
      if (
        !this.options.assistantLang.every(language => {
          return languages.includes(language);
        })
      ) {
        this.log.error(
          "ERROR: One of the languages is not recognized, please check your language value\n\t"
        );
        process.exit(1);
      }
    } else {
      this.log.error(
        "ERROR: Language must be selected from the list:\n\t" +
          languages.map(l => `${l.value} -> ${l.name}`).join("\n\t") +
          "\nDefault value: en-us"
      );
      process.exit(1);
    }

    // Generate the main prompts
    const prompts = [
      // Name of the assistant
      {
        type: `input`,
        name: `assistantName`,
        message: `What's the name of your assistant?`,
        default: this.options.assistantName
          ? this.options.assistantName
          : `sample-assistant`
      },
      // Description of the assistant
      {
        type: `input`,
        name: `assistantDesc`,
        message: `What's the description of your assistant?`,
        default: this.options.assistantDesc ? this.options.assistantDesc : ``
      },
      // Language of the assistant
      {
        type: "checkbox",
        name: "assistantLang",
        message: "Which languages will your assistant use?",
        choices: languagesChoice
      },
      // Path of the assistant
      {
        type: `confirm`,
        name: `pathConfirmation`,
        message: `Do you want to change the new assistant's location?`,
        default: false
      },
      {
        when: function(response) {
          return response.pathConfirmation === true;
        },
        type: `input`,
        name: `assistantGenerationPath`,
        message: `Where do you want to generate the assistant?`,
        default: this.options.assistantGenerationPath
          ? this.options.assistantGenerationPath
          : process.cwd(),
        validate: path => {
          if (fs.existsSync(path)) {
            return true;
          }

          this.log(
            chalk.red(
              `\n`,
              `ERROR: This is not a valid path. Please try again.`
            )
          );
        }
      },
      // Final confirmation of the assistant
      {
        type: `confirm`,
        name: `finalConfirmation`,
        message: `Looking good. Shall I go ahead and create your new assistant?`,
        default: true
      }
    ];

    // Check that if it was generated with CLI commands
    if (this.options.noPrompt) {
      this.props = _pick(this.options, [
        `assistantName`,
        `assistantDesc`,
        `assistantLang`,
        `assistantGenerationPath`
      ]);

      // Validate we have what we need, or we'll need to throw
      if (!this.props.assistantName) {
        this.log.error(
          `ERROR: Must specify a name for your assistant when using --noPrompt argument. Use --assistantName or -n option.`
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
    const assistantLang = this.props.assistantLang;
    if (this.props.finalConfirmation !== true) {
      return;
    }

    const assistantDesc = this.props.assistantDesc;
    if (!this.props.assistantName.replace(/\s/g, ``).length) {
      this.props.assistantName = templateName;
    }

    const assistantName = _kebabCase(this.props.assistantName).replace(
      /([^a-z0-9-]+)/gi,
      ``
    );

    const assistantNameCamelCase = _camelCase(this.props.assistantName).replace(
      /([^a-z0-9-]+)/gi,
      ``
    );

    if (this.props.assistantName !== assistantName) {
      finalAssistantName = assistantName;
      containsSpecialCharacter = true;
    }

    assistantGenerationPath = path.join(assistantGenerationPath, assistantName);
    if (this.props.assistantGenerationPath !== undefined) {
      assistantGenerationPath = path.join(
        this.props.assistantGenerationPath,
        assistantName
      );
    }

    if (fs.existsSync(assistantGenerationPath)) {
      isAlreadyCreated = true;
      return;
    }

    this.log(chalk.magenta(`\nCurrent values for the new assistant:`));
    this.log(chalk.magenta(`Name: ` + assistantName));
    this.log(chalk.magenta(`Description: ` + assistantDesc));
    this.log(chalk.magenta(`Selected languages: ` + assistantLang.join()));
    this.log(chalk.magenta(`Path: ` + assistantGenerationPath + `\n`));

    // Create new assistant obj
    const newAssistant = {
      assistantName: assistantName,
      assistantNameCamelCase: assistantNameCamelCase,
      assistantDescription: assistantDesc
    };

    // Start the copy of the template
    this.copier.selectLanguages(assistantLang);
    this.copier.copyTemplate(templateName, assistantGenerationPath);
    this.copier.copyTemplateFiles(
      templateName,
      assistantGenerationPath,
      newAssistant
    );
  }

  install() {
    if (this.props.finalConfirmation !== true || isAlreadyCreated) {
      return;
    }

    process.chdir(assistantGenerationPath);
    this.installDependencies({ npm: true, bower: false });
  }

  end() {
    if (this.props.finalConfirmation === true) {
      if (isAlreadyCreated) {
        this.log(
          chalk.red.bold(
            `-------------------------------------------------------------------------------------------- `
          )
        );
        this.log(
          chalk.red.bold(
            ` ERROR: It's seems like you already have an assistant with the same name in the destination path. `
          )
        );
        this.log(
          chalk.red.bold(
            ` Try again changing the name or the destination path or deleting the previous bot. `
          )
        );
        this.log(
          chalk.red.bold(
            `-------------------------------------------------------------------------------------------- `
          )
        );
      } else {
        this.spawnCommandSync("npm run build", []);
        if (containsSpecialCharacter) {
          this.log(
            chalk.yellow(
              `\nYour virtual assistant name (${this.props.assistantName}) had special characters, it was changed to '${finalAssistantName}'`
            )
          );
        }

        this.log(chalk.green(`------------------------ `));
        this.log(chalk.green(` Your new assistant is ready!  `));
        this.log(chalk.green(`------------------------ `));
        this.log(
          `Open the ` +
            chalk.green.bold(`README.md`) +
            ` to learn how to run your assistant. `
        );
        this.log(
          chalk.blue(
            `\nNext step - being in the root of your generated assistant, to deploy it execute the following command:`
          )
        );
        this.log(chalk.blue(`pwsh -File deployment\\scripts\\deploy.ps1`));
      }
    } else {
      this.log(chalk.red.bold(`-------------------------------- `));
      this.log(chalk.red.bold(` New assistant creation was canceled. `));
      this.log(chalk.red.bold(`-------------------------------- `));
    }

    this.log(`\nThank you for using the Microsoft Bot Framework. `);
    this.log(`\n${tinyBot} The Bot Framework Team`);
  }
};
