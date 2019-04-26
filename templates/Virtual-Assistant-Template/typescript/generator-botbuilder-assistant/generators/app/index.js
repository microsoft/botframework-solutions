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
const templateName = "customAssistant";
const languages = [`zh`, `de`, `en`, `fr`, `it`, `es`];
let assistantName;
let assistantDesc;
let assistantGenerationPath = process.cwd();
let isAlreadyCreated = false;
let copier;

const languagesChoice = [
  {
    name: "Chinese",
    value: "zh",
    checked: true
  },
  {
    name: "Deutsch",
    value: "de",
    checked: true
  },
  {
    name: "English",
    value: "en",
    checked: true
  },
  {
    name: "French",
    value: "fr",
    checked: true
  },
  {
    name: "Italian",
    value: "it",
    checked: true
  },
  {
    name: "Spanish",
    value: "es",
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
  `  │   BotBuilder Virtual Assistant  │\n` +
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
      default: `customAssistant`,
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
    copier = new Copier(this);
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
          "\nDefault value: en"
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
          : `customAssistant`
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

    assistantDesc = this.props.assistantDesc;
    if (!this.props.assistantName.replace(/\s/g, ``).length) {
      this.props.assistantName = templateName;
    }

    assistantName = _kebabCase(this.props.assistantName).replace(
      /([^a-z0-9-]+)/gi,
      ``
    );

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
      assistantDescription: assistantDesc
    };

    // Start the copy of the template
    copier.selectLanguages(assistantLang);
    copier.copyIgnoringTemplateFiles(templateName, assistantGenerationPath);
    copier.copyTemplateFiles(
      templateName,
      assistantGenerationPath,
      newAssistant
    );
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
        this.log(chalk.green(`------------------------ `));
        this.log(chalk.green(` Your new assistant is ready!  `));
        this.log(chalk.green(`------------------------ `));
        this.log(
          `Open the ` +
            chalk.green.bold(`README.md`) +
            ` to learn how to run your assistant. `
        );
      }
    } else {
      this.log(chalk.red.bold(`-------------------------------- `));
      this.log(chalk.red.bold(` New assistant creation was canceled. `));
      this.log(chalk.red.bold(`-------------------------------- `));
    }

    this.log(`Thank you for using the Microsoft Bot Framework. `);
    this.log(`\n` + tinyBot + `The Bot Framework Team`);
  }
};
