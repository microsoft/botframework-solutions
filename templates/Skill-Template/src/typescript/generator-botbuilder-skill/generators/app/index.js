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
    }

    prompting() {
        this.log(bigBot);
    }
}