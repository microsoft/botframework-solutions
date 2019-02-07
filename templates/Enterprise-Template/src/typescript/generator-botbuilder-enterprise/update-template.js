// Imports
const fs = require("fs");
const path = require("path");

// Path of the folder to get the new structure
const srcFolder = path.join(__dirname, "../enterprise-bot");
// Path of the folder to replace
const dstFolder = path.join(
  __dirname,
  "generators",
  "app",
  "templates",
  "enterprise-bot"
);
// Variable to catch every file which has not been copied
var uncopiedFiles = [];
copyTemplate(srcFolder, dstFolder);

// Copy the source folder to destination folder without the files which contains placeholders
function copyTemplate(srcFolder, dstFolder) {
  console.log(
    "****************************************************************"
  );
  console.log(
    "Starting to delete the files without placeholders in the template folder..."
  );
  deleteFiles(dstFolder);
  console.log("Finished deleting files.");
  console.log(
    "****************************************************************"
  );

  console.log(
    "****************************************************************"
  );
  console.log(
    "Starting to copy the files without placeholders in the template folder..."
  );
  copyFiles(srcFolder);
  printUncopiedFiles();
  console.log("Finished copying files.");
  console.log(
    "****************************************************************"
  );
}

// Function that deletes all the folders/files of the path, except those with placeholders
function deleteFiles(aPath) {
  fs.readdirSync(aPath).forEach(file => {
    const filePath = path.join(aPath, file);
    const fileStatus = fs.statSync(filePath);

    // If directory, execute a recursive call until find a file
    if (fileStatus && fileStatus.isDirectory()) {
      deleteFiles(filePath);
      try {
        fs.rmdirSync(filePath);
      } catch (error) {
        console.log(
          'The folder "' +
            filePath +
            "\" couldn't be deleted, as it may have files left inside. If this folders contains a file which name starts with a `_`, ignore this error."
        );
      }
      // It's a file which does not contain a placeholder
    } else if (!file.startsWith("_")) {
      try {
        // Delete the file
        fs.unlinkSync(filePath);
      } catch (error) {
        console.log(
          'The file "' +
            filePath +
            "\" couldn't be deleted. More info: " +
            error
        );
      }
    }
  });
}

// Function that copies the folders/files of the source path to the template path, except those with placeholders
function copyFiles(aPath) {
  fs.readdirSync(aPath).forEach(file => {
    const filePath = path.join(aPath, file);
    const fileStatus = fs.statSync(filePath);

    // If directory, execute a recursive call until find a file
    if (fileStatus && fileStatus.isDirectory()) {
      if (
        !fs.existsSync(path.join(dstFolder, path.relative(srcFolder, filePath)))
      ) {
        fs.mkdirSync(path.join(dstFolder, path.relative(srcFolder, filePath)));
      }

      copyFiles(filePath);

      // It's a file which contains a placeholder
    } else if (
      fs.existsSync(
        path.join(dstFolder, path.relative(srcFolder, aPath), "_" + file)
      )
    ) {
      // Add the file which contains placeholders to the array of uncopied files
      uncopiedFiles.push(file);
    } else {
      try {
        // Copy the file
        fs.copyFileSync(
          filePath,
          path.join(dstFolder, path.relative(srcFolder, aPath), file)
        );
      } catch (error) {
        console.log(
          'The file "' + filePath + "\" couldn't be copied. More info: " + error
        );
      }
    }
  });
}

function printUncopiedFiles() {
  if (uncopiedFiles) {
    console.log(
      "The following files were not updated automatically as it has placeholders:"
    );
    uncopiedFiles.forEach(uncopiedFile => console.log("- " + uncopiedFile));
  }
}
