'use strict';
const Generator = require('yeoman-generator');
const chalk = require('chalk');
const pkg = require('../../package.json');

const _pick = require('lodash/pick');
const _camelCase = require('lodash/camelCase');
const _upperFirst = require('lodash/upperFirst');
const _kebabCase = require('lodash/kebabCase');

const languages = [
  {
    name: 'German',
    value: 'de',
  },
  {
    name: 'English',
    value: 'en',
  },
  {
    name: 'Spanish',
    value: 'es',
  },
  {
    name: 'French',
    value: 'fr',
  },
  {
    name: 'Italian',
    value: 'it',
  },
  {
    name: 'Chinese',
    value: 'zh',
  },
];

const bigBot = 
'               ╭─────────────────────────╮\n'+
'   ' + chalk.blue.bold('//') + '     ' + chalk.blue.bold('\\\\') + '   │     Welcome to the      │\n' +
'  ' + chalk.blue.bold('//') + ' () () ' + chalk.blue.bold('\\\\') + '  │  BotBuilder Enterprise  │\n' +
'  ' + chalk.blue.bold('\\\\') + '       ' + chalk.blue.bold('//') + ' /│        generator!       │\n' +
'   ' + chalk.blue.bold('\\\\') + '     ' + chalk.blue.bold('//') + '   ╰─────────────────────────╯\n' +
`                                    v${pkg.version}`;

const tinyBot = ' ' + chalk.blue.bold('<') + ' ** ' + chalk.blue.bold('>') + ' ';

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.option('botName', {
      description: 'The name you want to give to your bot',
      type: String,
      default: 'enterprise-bot',
      alias: 'n'
    });

    this.option('botDesc', {
      description: 'A brief bit of text used to describe what your bot does',
      type: String,
      alias: 'd'
    });

    this.option('botLang', {
      description: 'The language you want to use with your bot.',
      type: String,
      default: 'en',
      alias: 'l'
    });

    this.option('noPrompt', {
      description: 'Do not prompt for any information or confirmation',
      type: Boolean,
      default: false
    });
  }

  prompting() {
    this.log(bigBot);

    // Validate language option
    if (this.options.botLang && !languages.some(l => l.value === this.options.botLang)) {
      this.log.error('ERROR: Language must be selected from the list:\n\t' + languages.map(l => `${l.value} -> ${l.name}`).join('\n\t') + '\nDefault value: en');
      process.exit(1);
    }

    const selectedLang = languages.findIndex(l => l.value === this.options.botLang);
    
    const prompts = [
      {
        type: 'input',
        name: 'botName',
        message: `What's the name of your bot?`,
        default: this.options.botName ? this.options.botName : 'enterprise-bot'
      },
      {
        type: 'input',
        name: 'botDesc',
        message: 'What will your bot do?',
        default: this.options.botDesc ? this.options.botDesc : 'Demonstrate advanced capabilities of a Conversational AI bot'
      },
      {
        type: 'list',
        name: 'botLang',
        message: 'What language will your bot use?',
        choices: languages,
        default: selectedLang
      },
      {
        name: 'finalConfirmation',
        type: 'confirm',
        message: 'Looking good. Shall I go ahead and create your new bot?',
        default: true
      }
    ];

    if (this.options.noPrompt) {
      this.props = _pick(this.options, ['botName', 'botDesc', 'botLang']);

      // validate we have what we need, or we'll need to throw
      if(!this.props.botName) {
        this.log.error('ERROR: Must specify a name for your bot when using --noPrompt argument. Use --botName or -n option.');
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

    const templateName = 'enterprise-bot';
    const botLang = this.props.botLang;
    const botDesc = this.props.botDesc;
    const botName = _kebabCase(this.props.botName);
    const botName_pascalCase = _upperFirst(_camelCase(this.props.botName));
    const botName_camelCase = _camelCase(this.props.botName);

    this.log('Current values:');
    this.log(botName);
    this.log(botName_camelCase);
    this.log(botDesc);
    this.log(botLang);

    this.fs.copy(
      this.templatePath(templateName, 'cognitiveModels', 'LUIS', botLang, '*'),
      this.destinationPath('cognitiveModels', 'LUIS')
    );

    this.fs.copy(
      this.templatePath(templateName, 'cognitiveModels', 'QnA', botLang, '*'),
      this.destinationPath('cognitiveModels', 'QnA')
    );

    this.fs.copy(
      this.templatePath(templateName, 'deploymentScripts', botLang, '*'),
      this.destinationPath('deploymentScripts')
    );

    this.fs.copyTpl(
      this.templatePath(templateName, '_package.json'),
      this.destinationPath('package.json'), {
        name: botName,
        description: botDesc
      }
    );

    this.fs.copyTpl(
      this.templatePath(templateName, 'src', '_index.ts'),
      this.destinationPath('src', 'index.ts'), {
        name: botName,
        description: botDesc,
        botNameClass: botName_pascalCase,
        botNameFile: botName_camelCase
      }
    );

    this.fs.copyTpl(
      this.templatePath(templateName, 'src', '_enterpriseBot.ts'),
      this.destinationPath('src', `${botName_camelCase}.ts`), {
        name: botName,
        description: botDesc,
        botNameClass: botName_pascalCase,
        botNameFile: botName_camelCase
      }
    );

    this.fs.copy(
      this.templatePath(templateName, 'src', 'botServices.ts'),
      this.destinationPath('src', 'botServices.ts')
    );

    const commonFiles = [ '.env.development', '.env.production', '.gitignore', 'README.md', 'tsconfig.json', 'deploymentScripts/webConfigPrep.js' ];

    commonFiles.forEach(fileName => this.fs.copy(
      this.templatePath(templateName, fileName),
      this.destinationPath(fileName)
    ));

    const commonDirectories = [ 'dialogs', 'extensions', 'locales', 'middleware', 'serviceClients' ];

    commonDirectories.forEach(directory => this.fs.copy(
      this.templatePath(templateName, 'src', directory, '**', '*'),
      this.destinationPath('src', directory)
    ));
  }

  install() {
    if (this.props.finalConfirmation !== true) {
      return;
    }

    this.installDependencies({ npm: true, bower: false });
  }

  end() {
    if (this.props.finalConfirmation === true) {
      this.log(chalk.green('------------------------ '));
      this.log(chalk.green(' Your new bot is ready!  '));
      this.log(chalk.green('------------------------ '));
      this.log('Open the ' + chalk.green.bold('README.md') + ' to learn how to run your bot. ');
    } else {
      this.log(chalk.red.bold('-------------------------------- '));
      this.log(chalk.red.bold(' New bot creation was canceled. '));
      this.log(chalk.red.bold('-------------------------------- '));
    }

    this.log('Thank you for using the Microsoft Bot Framework. ');
    this.log('\n' + tinyBot + 'The Bot Framework Team');
  }
};
