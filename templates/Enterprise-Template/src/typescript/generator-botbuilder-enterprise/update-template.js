const fs = require('fs');
const path = require("path");
const testFolder = path.join(__dirname, 'generators/app/templates/enterprise-bot');
const srcFolder = path.join(__dirname, '../enterprise-bot');

console.log('Starting to delete files without placeholders...');
readPath(testFolder);
console.log('Finished deleting files...');

console.log('Starting to copy files without placeholders...');
readPath2(srcFolder);
console.log('Finished copying files...');

function readPath(pathToRead) {
    fs.readdirSync(pathToRead).forEach(file => {
        const filePath = path.join(pathToRead, file);
        const stat = fs.statSync(filePath);
        // If directory, execute a recursive call
        if (stat && stat.isDirectory()) {
            readPath(filePath);
            try {
                fs.rmdirSync(filePath);
            }
            catch(error) {
                console.log('The folder "' + filePath + '" couldn\'t be deleted, as it may have files left inside. If the files\' name starts with a `_`, ignore this error.')
            }
        } else {
            if (file.startsWith('_')) {
                console.log('The file "' + file + '" located in "' +  filePath + '" won\'t be updated automatically as it has placeholders.');
            } else {
                try {
                    fs.unlinkSync(filePath);
                } catch (error) {
                    console.log('The file "' + filePath + '" couldn\'t be deleted. More info: ' + error);
                }
            }
        }
    });
}

function readPath2(pathToRead) {
    fs.readdirSync(pathToRead).forEach(file => {
        const filePath = path.join(pathToRead, file);
        const stat = fs.statSync(filePath);
        if (stat && stat.isDirectory()) {
            if (!fs.existsSync(path.join(testFolder, path.relative(srcFolder, filePath)))) {
                fs.mkdirSync(path.join(testFolder, path.relative(srcFolder, filePath)));
            }
            readPath2(filePath);
        } else {
            if (fs.existsSync(path.join(testFolder, path.relative(srcFolder, pathToRead), '_' + file))) {
                console.log('File "' + file + '" was not copied because it\'s already present in destination folder with placeholders. Please review it yourself.');
            } else {
                fs.copyFileSync(filePath, path.join(testFolder, path.relative(srcFolder, pathToRead), file));
            }
        }
    });
}
