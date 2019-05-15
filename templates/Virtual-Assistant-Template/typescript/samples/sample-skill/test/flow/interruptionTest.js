/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const skillTestBase = require("./skillTestBase");
const testNock = require("../testBase");

describe("interruption", function() {
  beforeEach(async function() {
    await skillTestBase.initialize();
  });

  describe("help interruption", function() {
    it("send 'help' during the sample dialog", function(done) {
      const testAdapter = skillTestBase.getTestAdapter();
      const flow = testAdapter
        .send("sample dialog")
        .assertReply("What is your name?")
        .send("help")
        .assertReply("[Enter your help message here]");
      testNock.resolveWithMocks("interruption_help_response", done, flow);
    });
  });

  describe("cancel interruption", function() {
    it("send 'cancel' during the sample dialog", function(done) {
      const testAdapter = skillTestBase.getTestAdapter();
      const flow = testAdapter
        .send("sample dialog")
        .assertReply("What is your name?")
        .send("cancel")
        .assertReply("Ok, let's start over.");
      testNock.resolveWithMocks("interruption_cancel_response", done, flow);
    });
  });
});
