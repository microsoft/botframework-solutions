const { strictEqual } = require("assert");
const { getNormalizedFile } = require("./helpers/normalizeUtils");
const { ManifestUtils } = require("../lib/utils");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");


const manifestUtil = new ManifestUtils();
const genericManifest = getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "genericManifest.json")));
const manifestV1 = getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v1", "manifest.json")));
const manifestV2 = getNormalizedFile(resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")));

describe("The Manifest Util", function () {
    beforeEach(function() {
        this.logger = new testLogger.TestLogger();
    });

    describe("should show an error when", function () {
        it("a manifest v1 has missing properties", async function () {
            const configuration = {
                localManifest: resolve(__dirname, join("mocks", "manifests", "v1", "invalidManifest.json")),
                remoteManifest: "",
                logger: this.logger
            };

            try {
                const invalidManifest = await manifestUtil.getRawManifestFromResource(configuration);
                await manifestUtil.getManifest(invalidManifest, this.logger)
            } catch (error) {
                strictEqual(error.message, `One or more properties are missing from your Skill Manifest`);
            }
        });

        it("a manifest v2 has missing properties", async function () {
            const configuration = {
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "invalidManifest.json")),
                remoteManifest: "",
                logger: this.logger
            };

            try {
                const invalidManifest = await manifestUtil.getRawManifestFromResource(configuration);
                await manifestUtil.getManifest(invalidManifest, this.logger)
            } catch (error) {
                strictEqual(error.message, `One or more properties are missing from your Skill Manifest`);
            }
        });

        it("it can't determine the schema version", async function () {
            const undeterminedManifest = JSON.stringify({
                "$schema": "",
                "endpoints": [ ],
                "dispatchModels": { },
                "activities": { }
            });

            try {
                await manifestUtil.getManifest(undeterminedManifest, this.logger)
            } catch (error) {
                strictEqual(error.message, `Your Skill Manifest is not compatible. Please note that the minimum supported manifest version is 2.1.`);
            }
        });
    });

    describe("should be able to read", function () {
        it("a local manifest with absolute path", async function () {
            const configuration = {
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                logger: this.logger
            };

            const rawResource = await manifestUtil.getRawManifestFromResource(configuration);

            strictEqual(manifestV2, rawResource);
        });

        it("a local manifest with relative path", async function () {
            const configuration = {
                localManifest: join("test", "mocks", "manifests", "v2", "manifest.json"),
                remoteManifest: "",
                logger: this.logger
            };

            const rawResource = await manifestUtil.getRawManifestFromResource(configuration);

            strictEqual(manifestV2, rawResource);
        });

        it("a remote manifest", async function () {
            sandbox.replace(manifestUtil, "getRemoteManifest", (command, args) => {
                return Promise.resolve(manifestV2);
            });
            
            const configuration = {
                localManifest: "",
                remoteManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                logger: this.logger
            };

            const rawResource = await manifestUtil.getRawManifestFromResource(configuration);

            strictEqual(manifestV2, rawResource);
        });
    });

    describe("should be able to parse", function () {
        it("a manifest v1", async function () {
            const parsedManifest = await manifestUtil.getManifest(manifestV1, this.logger);
            let manifest = JSON.parse(genericManifest);
            manifest.schema = "";
            manifest.version = "";
            manifest.entries = undefined;
            manifest.allowedIntents = ['*'];
            manifest.luisDictionary = new Map(JSON.parse(manifest.luisDictionary));
  
            strictEqual(JSON.stringify(manifest), JSON.stringify(parsedManifest));
        });

        it("a manifest v2", async function () {
            const parsedManifest = await manifestUtil.getManifest(manifestV2, this.logger);
            let manifest = JSON.parse(genericManifest);
            manifest.luisDictionary = new Map(JSON.parse(manifest.luisDictionary));
  
            strictEqual(JSON.stringify(manifest), JSON.stringify(parsedManifest));
        });
    });
});