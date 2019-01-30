var Generator = require("yeoman-generator");
const chalk = require("chalk");
const path = require("path");
const _pick = require("lodash/pick");
const pkg = require("../../package.json");
const fs = require("fs");

const bigBot =
  "               ╭─────────────────────────────────╮\n" +
  "   " +
  chalk.blue.bold("//") +
  "     " +
  chalk.blue.bold("\\\\") +
  "   │         Welcome to the          │\n" +
  "  " +
  chalk.blue.bold("//") +
  " () () " +
  chalk.blue.bold("\\\\") +
  "  │  BotBuilder Enterprise Dialogs  │\n" +
  "  " +
  chalk.blue.bold("\\\\") +
  "       " +
  chalk.blue.bold("//") +
  " /│           generator!            │\n" +
  "   " +
  chalk.blue.bold("\\\\") +
  "     " +
  chalk.blue.bold("//") +
  "   ╰─────────────────────────────────╯\n" +
  `                                    v${pkg.version}`;

var dialogsPath = process.cwd();

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option("dialogsPath", {
      description: "The path where the dialog will be generated.",
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

    const prompts = [
      {
        type: "input",
        name: "dialogsPath",
        message: `Where do you want to generate the dialog?`,
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
        message: "Looking good. Shall I go ahead and create your new dialog?",
        default: true
      }
    ];

    if (this.options.noPrompt) {
      this.props = _pick(this.options, ["dialogsPath"]);

      this.props.finalConfirmation = true;
      return;
    }

    return this.prompt(prompts).then(props => {
      this.props = props;
    });
  }

  writing() {
    const templateName = "customDialog";
    if (this.props.finalConfirmation !== true) {
      return;
    }

    if (this.props.dialogsPath !== undefined) {
      dialogsPath = path.join(this.props.dialogsPath);
    }

    this.fs.copyTpl(
      this.templatePath(templateName),
      this.destinationPath(dialogsPath, templateName)
    );
  }
};
