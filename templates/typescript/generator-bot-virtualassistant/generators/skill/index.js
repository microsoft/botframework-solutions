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
const templateName = "sample-skill";
const languages = [`zh-cn`, `de-de`, `en-us`, `fr-fr`, `it-it`, `es-es`];
let skillGenerationPath = process.cwd();
let isAlreadyCreated = false;
let containsSpecialCharacter = false;
let finalSkillName = "";

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
  `  │       Bot Skill       │\n` +
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
      default: `sample-skill`,
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
    this.copier = new Copier(this);
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
          "\nDefault value: en-us"
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
        default: this.options.skillName
          ? this.options.skillName
          : `sample-skill`
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

    const skillDesc = this.props.skillDesc;
    if (!this.props.skillName.replace(/\s/g, ``).length) {
      this.props.skillName = templateName;
    }

    const skillName = _kebabCase(this.props.skillName).replace(
      /([^a-z0-9-]+)/gi,
      ``
    );
    skillGenerationPath = path.join(skillGenerationPath, skillName);
    if (this.props.skillGenerationPath !== undefined) {
      skillGenerationPath = path.join(
        this.props.skillGenerationPath,
        skillName
      );
    }

    if (this.props.skillName !== skillName) {
      finalSkillName = skillName;
      containsSpecialCharacter = true;
    }

    const skillNameCamelCase = _camelCase(this.props.skillName).replace(
      /([^a-z0-9-]+)/gi,
      ``
    );

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
      skillNameCamelCase: skillNameCamelCase,
      skillDescription: skillDesc
    };

    // Start the copy of the template
    this.copier.selectLanguages(skillLang);
    this.copier.copyTemplate(templateName, skillGenerationPath);
    this.copier.copyTemplateFiles(templateName, skillGenerationPath, newSkill);
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
        this.spawnCommandSync("npm run build", []);
        if (containsSpecialCharacter) {
          this.log(
            chalk.yellow(
              `\nYour skill name (${this.props.skillName}) had special characters, it was changed to '${finalSkillName}'`
            )
          );
        }

        this.log(chalk.green(`------------------------ `));
        this.log(chalk.green(` Your new skill is ready!  `));
        this.log(chalk.green(`------------------------ `));
        this.log(
          `Open the ` +
            chalk.green.bold(`README.md`) +
            ` to learn how to run your skill. `
        );
        this.log(
          chalk.blue(
            `\nNext step - being in the root of your generated skill, to deploy it execute the following command:`
          )
        );
        this.log(chalk.blue(`pwsh -File deployment\\scripts\\deploy.ps1`));
      }
    } else {
      this.log(chalk.red.bold(`-------------------------------- `));
      this.log(chalk.red.bold(` New skill creation was canceled. `));
      this.log(chalk.red.bold(`-------------------------------- `));
    }

    this.log(`\nThank you for using the Microsoft Bot Framework. `);
    this.log(`\n${tinyBot} The Bot Framework Team`);
  }
};
