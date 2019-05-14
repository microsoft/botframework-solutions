/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const assert = require("assert");
const skillTestBase = require("./skillTestBase");
const testNock = require("../testBase");
const unhandledReplies = [
  "Can you try to ask me again? I didn't get what you mean.",
  "Can you say that in a different way?",
  "Can you try to ask in a different way?",
  "Could you elaborate?",
  "Please say that again in a different way.",
  "I didn't understand, perhaps try again in a different way.",
  "I didn't get what you mean, can you try in a different way?",
  "Sorry, I didn't understand what you meant.",
  "I didn't quite get that."
];

describe("main dialog", function() {
  beforeEach(async function() {
    await skillTestBase.initialize();
  });

  describe("intro message", function() {
    it("send conversationUpdate and check the intro message is received", function(done) {
      const testAdapter = skillTestBase.getTestAdapter();
      const flow = testAdapter
        .send({
          type: "conversationUpdate",
          membersAdded: [
            {
              id: "1",
              name: "Bot"
            }
          ],
          channelId: "emulator",
          recipient: {
            id: "1"
          },
          locale: "en"
        })
        .assertReply("[Enter your intro message here]");

      testNock.resolveWithMocks("mainDialog_intro_response", done, flow);
    });
  });

  describe("help intent", function() {
    it("send 'help' and check you get the expected response", function(done) {
      const testAdapter = skillTestBase.getTestAdapter();
      const flow = testAdapter
        .send("help")
        .assertReply("[Enter your help message here]");

      testNock.resolveWithMocks("mainDialog_help_response", done, flow);
    });
  });

  describe("test unhandled message", function() {
    it("send 'blah blah' and check you get the expected response", function(done) {
      const testAdapter = skillTestBase.getTestAdapter();
      const flow = testAdapter
        .send("blah blah")
        .assertReply(function(activity) {
          assert.notEqual(-1, unhandledReplies.indexOf(activity.text));
        });

      testNock.resolveWithMocks("mainDialog_unhandled_response", done, flow);
    });
  });
});
