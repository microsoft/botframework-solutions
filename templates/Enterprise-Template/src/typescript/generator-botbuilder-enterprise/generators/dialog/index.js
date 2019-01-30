"use strict";
var Generator = require("yeoman-generator");
const chalk = require("chalk");
const path = require("path");
const _pick = require("lodash/pick");
const pkg = require("../../package.json");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const fs = require("fs");

const dialogRegExp = /\w*dialog\b/i;
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

const tinyBot =
  " " + chalk.blue.bold("<") + " ** " + chalk.blue.bold(">") + " ";

var dialogGenerationPath = process.cwd();
var isAlreadyCreated = false;

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option("dialogName", {
      description: "The name you want to give to your dialog.",
      type: String,
      optional: false,
      alias: "n"
    });

    this.option("dialogGenerationPath", {
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
        name: "dialogName",
        message: `What's the name of your dialog?`,
        default: this.options.dialogName ? this.options.dialogName : "custom",
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
        name: "dialogGenerationPath",
        message: `Where do you want to generate the dialog?`,
        default: this.options.dialogGenerationPath
          ? this.options.dialogGenerationPath
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
      this.props = _pick(this.options, [
        "dialogName",
        "dialogGenerationPath"
      ]);

      // Validate we have what we need, or we'll need to throw
      if (!this.props.dialogName) {
        this.log.error(
          "ERROR: Must specify a name for your dialog when using --noPrompt argument. Use --dialogName or -n option."
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
    const templateName = "customDialog";
    if (this.props.finalConfirmation !== true) {
      return;
    }

    if (this.props.dialogGenerationPath !== undefined) {
      dialogGenerationPath = path.join(this.props.dialogGenerationPath);
    }

    if (!this.props.dialogName.replace(/\s/g, "").length) {
      this.props.dialogName = templateName;
    }
   
    this.props.dialogName = this.props.dialogName.replace(
      /([^a-z0-9]+)/gi,
      ""
    );

    var dialogName, responsesName;
    const dialogNameFolder = this.props.dialogName
    if(dialogRegExp.test(this.props.dialogName)){
      dialogName = this.props.dialogName;
      responsesName = this.props.dialogName.slice(0,-6).concat("Responses");
    }
    else{
      dialogName = this.props.dialogName.concat("Dialog") ;
      responsesName = this.props.dialogName.concat("Responses");
    }

    const dialogNameCamelCase = _camelCase(dialogName);
    const dialogNamePascalCase = _upperFirst(_camelCase(dialogName));
    const responsesNameCamelCase = _camelCase(responsesName);
    const responsesNamePascalCase = _upperFirst(_camelCase(responsesName));
    dialogGenerationPath = path.join(dialogGenerationPath, dialogName);

    if (this.props.dialogGenerationPath !== undefined) {
      dialogGenerationPath = path.join(this.props.dialogGenerationPath, dialogNameFolder);
    }

    if (fs.existsSync(dialogGenerationPath)) {
      isAlreadyCreated = true;
      return;
    }

    this.log(chalk.magenta("\nCurrent values for the new dialog:"));
    this.log(chalk.magenta("Folder: " + dialogNameFolder));
    this.log(chalk.magenta("Dialog file: " + dialogName.concat(".ts")));
    this.log(chalk.magenta("Responses file: " + responsesName.concat(".ts")));
    this.log(chalk.magenta("Path: " + dialogGenerationPath + "\n"));

    this.fs.copyTpl(
      this.templatePath(templateName,"_dialog.ts"),
      this.destinationPath(dialogGenerationPath, `${dialogNameCamelCase}.ts`),
      {
        dialogNameClass: dialogNamePascalCase,
        responsesNameClass: responsesNamePascalCase,
        responsesNameFile: responsesNameCamelCase
      }
    );

    this.fs.copyTpl(
      this.templatePath(templateName,"_responses.ts"),
      this.destinationPath(dialogGenerationPath, `${responsesNameCamelCase}.ts`),
      {
        dialogNameClass: dialogNamePascalCase,
        responsesNameClass: responsesNamePascalCase,
        responsesNameFile: responsesNameCamelCase
      }
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
            " ERROR: It's seems like you already have a dialog with the same name in the destination path. "
          )
        );
        this.log(
          chalk.red.bold(
            " Try again changing the name or the destination path or deleting the previous dialog. "
          )
        );
        this.log(
          chalk.red.bold(
            "-------------------------------------------------------------------------------------------- "
          )
        );
      } else {
        this.log(chalk.green("------------------------ "));
        this.log(chalk.green(" Your new dialog is ready!  "));
        this.log(chalk.green("------------------------ "));
      }
    } else {
      this.log(chalk.red.bold("-------------------------------- "));
      this.log(chalk.red.bold(" New dialog creation was canceled. "));
      this.log(chalk.red.bold("-------------------------------- "));
    }

    this.log("Thank you for using the Microsoft Bot Framework. ");
    this.log("\n" + tinyBot + "The Bot Framework Team");
  }
};
