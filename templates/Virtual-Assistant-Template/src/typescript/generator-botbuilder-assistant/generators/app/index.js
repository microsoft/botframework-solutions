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
  "               ╭─────────────────────────────────╮\n" +
  "   " +
  chalk.blue.bold("//") +
  "     " +
  chalk.blue.bold("\\\\") +
  "   │        Welcome to the           │\n" +
  "  " +
  chalk.blue.bold("//") +
  " () () " +
  chalk.blue.bold("\\\\") +
  "  │   BotBuilder Virtual Assistant  │\n" +
  "  " +
  chalk.blue.bold("\\\\") +
  "       " +
  chalk.blue.bold("//") +
  " /│          generator!             │\n" +
  "   " +
  chalk.blue.bold("\\\\") +
  "     " +
  chalk.blue.bold("//") +
  "   ╰─────────────────────────────────╯\n" +
  `                                    v${pkg.version}`;

const tinyBot =
  " " + chalk.blue.bold("<") + " ** " + chalk.blue.bold(">") + " ";

var assistantGenerationPath = process.cwd();
var isAlreadyCreated = false;

module.exports = class extends Generator {
    constructor(args, opts) {
        super(args, opts); 
        
        this.option("assistantName", {
            description: "The name you want to give to your assistant.",
            type: String,
            default: "customAssistant",
            alias: "n"
        });

        this.option("assistantDesc", {
            description: "A brief bit of text used to describe what your assistant does.",
            type: String,
            alias: "d"
        });

        this.option("assistantGenerationPath", {
            description: "The path where the assistant will be generated.",
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
            // Name of the assistant
            {
            type: "input",
            name: "assistantName",
            message: "What's the name of your assistant?",
            default: this.options.assistantName ? this.options.assistantName : "customAssistant"
            },
            // Description of the assistant
            {
            type: "input",
            name: "assistantDesc",
            message: "What will your assistant do?",
            default: this.options.assistantDesc ? this.options.assistantDesc : ""
            },
            // Path of the assistant
            {
            type: "confirm",
            name: "pathConfirmation",
            message: "Do you want to change the new assistant's location?",
            default: false
            },
            {
            when: function(response) {
                return response.pathConfirmation === true;
            },
            type: "input",
            name: "assistantGenerationPath",
            message: "Where do you want to generate the assistant?",
            default: this.options.assistantGenerationPath
                ? this.options.assistantGenerationPath
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
            // Final confirmation of the assistant
            {
            type: "confirm",
            name: "finalConfirmation",
            message: "Looking good. Shall I go ahead and create your new assistant?",
            default: true
            }
        ];

        // Check that if it was generated with CLI commands
        if (this.options.noPrompt) {
            this.props = _pick(this.options, [
            "assistantName",
            "assistantDesc",
            "assistantGenerationPath"
            ]);
    
            // Validate we have what we need, or we'll need to throw
            if (!this.props.assistantName) {
            this.log.error(
                "ERROR: Must specify a name for your assistant when using --noPrompt argument. Use --assistantName or -n option."
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

    }

    end() {
    
    }
}