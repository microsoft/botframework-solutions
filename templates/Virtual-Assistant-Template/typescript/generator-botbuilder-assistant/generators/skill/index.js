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
const templateName = "customSkill";
const languages = [`zh`, `de`, `en`, `fr`, `it`, `es`];
let skillName;
let skillDesc;
let skillGenerationPath = process.cwd();
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
  `               ╭───────────────────────╮\n` +
  `   ` +
  chalk.blue.bold(`//`) +
  `     ` +
  chalk.blue.bold(`\\\\`) +
  `   │     Welcome to the    │\n` +
  `  ` +
  chalk.blue.bold(`//`) +
  ` () () ` +
  chalk.blue.bold(`\\\\`) +
  `  │    BotBuilder Skill   │\n` +
  `  ` +
  chalk.blue.bold(`\\\\`) +
  `       ` +
  chalk.blue.bold(`//`) +
  ` /│       generator!      │\n` +
  `   ` +
  chalk.blue.bold(`\\\\`) +
  `     ` +
  chalk.blue.bold(`//`) +
  `   ╰───────────────────────╯\n` +
  `                                    v${pkg.version}`;

const tinyBot =
  ` ` + chalk.blue.bold(`<`) + ` ** ` + chalk.blue.bold(`>`) + ` `;

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option(`skillName`, {
      description: `The name you want to give to your skill.`,
      type: String,
      default: `customSkill`,
      alias: `n`
    });

    this.option(`skillDesc`, {
      description: `A brief bit of text used to describe what your skill does.`,
      type: String,
      alias: `d`
    });

    this.option(`skillLang`, {
      description: `The languages you want to use with your skill.`,
      type: String,
      alias: `l`,
      default: languages.join()
    });

    this.option(`skillGenerationPath`, {
      description: `The path where the skill will be generated.`,
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
    if (this.options.skillLang) {
      this.options.skillLang = this.options.skillLang
        .replace(/\s/g, "")
        .split(",");
      if (
        !this.options.skillLang.every(language => {
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
      // Name of the skill
      {
        type: `input`,
        name: `skillName`,
        message: `What's the name of your skill?`,
        default: this.options.skillName ? this.options.skillName : `customSkill`
      },
      // Description of the skill
      {
        type: `input`,
        name: `skillDesc`,
        message: `What's the description of your skill?`,
        default: this.options.skillDesc ? this.options.skillDesc : ``
      },
      // Language of the skill
      {
        type: "checkbox",
        name: "skillLang",
        message: "Which languages will your skill use?",
        choices: languagesChoice
      },
      // Path of the skill
      {
        type: `confirm`,
        name: `pathConfirmation`,
        message: `Do you want to change the new skill's location?`,
        default: false
      },
      {
        when: function(response) {
          return response.pathConfirmation === true;
        },
        type: `input`,
        name: `skillGenerationPath`,
        message: `Where do you want to generate the skill?`,
        default: this.options.skillGenerationPath
          ? this.options.skillGenerationPath
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
      // Final confirmation of the skill
      {
        type: `confirm`,
        name: `finalConfirmation`,
        message: `Looking good. Shall I go ahead and create your new skill?`,
        default: true
      }
    ];

    // Check that if it was generated with CLI commands
    if (this.options.noPrompt) {
      this.props = _pick(this.options, [
        `skillName`,
        `skillDesc`,
        `skillLang`,
        `skillGenerationPath`
      ]);

      // Validate we have what we need, or we'll need to throw
      if (!this.props.skillName) {
        this.log.error(
          `ERROR: Must specify a name for your skill when using --noPrompt argument. Use --skillName or -n option.`
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
    const skillLang = this.props.skillLang;
    if (this.props.finalConfirmation !== true) {
      return;
    }

    skillDesc = this.props.skillDesc;
    if (!this.props.skillName.replace(/\s/g, ``).length) {
      this.props.skillName = templateName;
    }

    skillName = _kebabCase(this.props.skillName).replace(/([^a-z0-9-]+)/gi, ``);

    skillGenerationPath = path.join(skillGenerationPath, skillName);
    if (this.props.skillGenerationPath !== undefined) {
      skillGenerationPath = path.join(
        this.props.skillGenerationPath,
        skillName
      );
    }

    if (fs.existsSync(skillGenerationPath)) {
      isAlreadyCreated = true;
      return;
    }

    this.log(chalk.magenta(`\nCurrent values for the new skill:`));
    this.log(chalk.magenta(`Name: ` + skillName));
    this.log(chalk.magenta(`Description: ` + skillDesc));
    this.log(chalk.magenta(`Selected languages: ` + skillLang.join()));
    this.log(chalk.magenta(`Path: ` + skillGenerationPath + `\n`));

    // Create new skill obj
    const newSkill = {
      skillName: skillName,
      skillDescription: skillDesc
    };

    // Start the copy of the template
    copier.selectLanguages(skillLang);
    copier.copyIgnoringTemplateFiles(templateName, skillGenerationPath);
    copier.copyTemplateFiles(templateName, skillGenerationPath, newSkill);
  }

  install() {
    if (this.props.finalConfirmation !== true || isAlreadyCreated) {
      return;
    }

    process.chdir(skillGenerationPath);
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
            ` ERROR: It's seems like you already have an skill with the same name in the destination path. `
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
        this.log(chalk.green(` Your new skill is ready!  `));
        this.log(chalk.green(`------------------------ `));
        this.log(
          `Open the ` +
            chalk.green.bold(`README.md`) +
            ` to learn how to run your skill. `
        );
      }
    } else {
      this.log(chalk.red.bold(`-------------------------------- `));
      this.log(chalk.red.bold(` New skill creation was canceled. `));
      this.log(chalk.red.bold(`-------------------------------- `));
    }

    this.log(`Thank you for using the Microsoft Bot Framework. `);
    this.log(`\n` + tinyBot + `The Bot Framework Team`);
  }
};
