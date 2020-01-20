/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const skillTestBase = require("./helpers/skillTestBase");
const testNock = require("./helpers/testBase");
const nameInput = "custom";
const sampleDialogNameReplies = [
  `Hi, ${nameInput}!`,
  `Nice to meet you, ${nameInput}!`
];

describe("sample dialog", function() {
  beforeEach(async function() {
    await skillTestBase.initialize();
  });

  describe("sample intent", function() {
    it("send 'sample dialog' and check you get the expected response", function(done) {
      const testAdapter = skillTestBase.getTestAdapter();
      const flow = testAdapter
        .send("sample dialog")
        .assertReply("What is your name?")
        .send(nameInput)
        .assertReply(function(activity) {
          assert.notStrictEqual(-1, sampleDialogNameReplies.indexOf(activity.text));
        });

      testNock.resolveWithMocks("sampleDialog_response", done, flow);
    });
  });
});
