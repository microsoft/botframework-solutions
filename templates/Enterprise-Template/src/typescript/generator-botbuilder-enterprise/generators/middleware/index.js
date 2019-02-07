"use strict";
var Generator = require("yeoman-generator");
const chalk = require("chalk");
const path = require("path");
const _pick = require("lodash/pick");
const pkg = require("../../package.json");
const _camelCase = require("lodash/camelCase");
const _upperFirst = require("lodash/upperFirst");
const fs = require("fs");

const middlewareRegExp = /\w*middleware\b/i;
const bigBot =
  "               ╭───────────────────────────────────╮\n" +
  "   " +
  chalk.blue.bold("//") +
  "     " +
  chalk.blue.bold("\\\\") +
  "   │         Welcome to the            │\n" +
  "  " +
  chalk.blue.bold("//") +
  " () () " +
  chalk.blue.bold("\\\\") +
  "  │  BotBuilder Enterprise Middlewares│\n" +
  "  " +
  chalk.blue.bold("\\\\") +
  "       " +
  chalk.blue.bold("//") +
  " /│           generator!              │\n" +
  "   " +
  chalk.blue.bold("\\\\") +
  "     " +
  chalk.blue.bold("//") +
  "   ╰───────────────────────────────────╯\n" +
  `                                    v${pkg.version}`;

const tinyBot =
  " " + chalk.blue.bold("<") + " ** " + chalk.blue.bold(">") + " ";

var middlewareGenerationPath = process.cwd();
var isAlreadyCreated = false;

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option("middlewareName", {
      description: "The name you want to give to your middleware.",
      type: String,
      optional: false,
      alias: "n"
    });

    this.option("middlewareGenerationPath", {
      description: "The path where the middleware will be generated.",
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
        name: "middlewareName",
        message: `What's the name of your middleware?`,
        default: this.options.middlewareName
          ? this.options.middlewareName
          : "custom"
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
        name: "middlewareGenerationPath",
        message: `Where do you want to generate the middleware?`,
        default: this.options.middlewareGenerationPath
          ? this.options.middlewareGenerationPath
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
        message:
          "Looking good. Shall I go ahead and create your new middleware?",
        default: true
      }
    ];

    if (this.options.noPrompt) {
      this.props = _pick(this.options, [
        "middlewareName",
        "middlewareGenerationPath"
      ]);

      // Validate we have what we need, or we'll need to throw
      if (!this.props.middlewareName) {
        this.log.error(
          "ERROR: Must specify a name for your middleware when using --noPrompt argument. Use --middlewareName or -n option."
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
    const templateName = "customMiddleware";
    if (this.props.finalConfirmation !== true) {
      return;
    }

    if (this.props.middlewareGenerationPath !== undefined) {
      middlewareGenerationPath = path.join(this.props.middlewareGenerationPath);
    }

    if (!this.props.middlewareName.replace(/\s/g, "").length) {
      this.props.middlewareName = templateName;
    }

    this.props.middlewareName = this.props.middlewareName.replace(
      /([^a-z0-9]+)/gi,
      ""
    );

    var middlewareName;
    const middlewareNameFolder = this.props.middlewareName;
    if (middlewareRegExp.test(this.props.middlewareName)) {
      middlewareName = this.props.middlewareName;
    } else {
      middlewareName = this.props.middlewareName.concat("Middleware");
    }

    const middlewareNameCamelCase = _camelCase(middlewareName);
    const middlewareNamePascalCase = _upperFirst(_camelCase(middlewareName));
    middlewareGenerationPath = path.join(
      middlewareGenerationPath,
      middlewareName
    );

    if (this.props.middlewareGenerationPath !== undefined) {
      middlewareGenerationPath = path.join(
        this.props.middlewareGenerationPath,
        middlewareNameFolder
      );
    }

    if (fs.existsSync(middlewareGenerationPath)) {
      isAlreadyCreated = true;
      return;
    }

    this.log(chalk.magenta("\nCurrent values for the new middleware:"));
    this.log(chalk.magenta("Folder: " + middlewareNameFolder));
    this.log(chalk.magenta("Middleware file: " + middlewareName.concat(".ts")));
    this.log(chalk.magenta("Path: " + middlewareGenerationPath + "\n"));

    this.fs.copyTpl(
      this.templatePath(templateName, "_middleware.ts"),
      this.destinationPath(
        middlewareGenerationPath,
        `${middlewareNameCamelCase}.ts`
      ),
      {
        middlewareNameClass: middlewareNamePascalCase
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
            " ERROR: It's seems like you already have a middleware with the same name in the destination path. "
          )
        );
        this.log(
          chalk.red.bold(
            " Try again changing the name or the destination path or deleting the previous middleware. "
          )
        );
        this.log(
          chalk.red.bold(
            "-------------------------------------------------------------------------------------------- "
          )
        );
      } else {
        this.log(chalk.green("------------------------ "));
        this.log(chalk.green(" Your new middleware is ready!  "));
        this.log(chalk.green("------------------------ "));
      }
    } else {
      this.log(chalk.red.bold("-------------------------------- "));
      this.log(chalk.red.bold(" New middleware creation was canceled. "));
      this.log(chalk.red.bold("-------------------------------- "));
    }

    this.log("Thank you for using the Microsoft Bot Framework. ");
    this.log("\n" + tinyBot + "The Bot Framework Team");
  }
};
