// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const assert = require('assert');
const <%=skillTemplateName%> = require('./<%=skillTemplateNameFile%>');
const testNock = require('../testBase');
let testAdapter;

describe("interruption", function() {
    before(async function() {
        await <%=skillTemplateName%>.initialize();
        testAdapter = <%=skillTemplateName%>.getTestAdapter();
    });

    describe("help", function() {
        it("send 'Help' and check you get the expected response", function(done){
            const flow = testAdapter
                .send('Help')
                .assertReply('[Enter your help message here]');
            
            testNock.resolveWithMocks('helpInterruption_response', done, flow);
        }); 
    });

    describe("cancel", function() {
        it("send 'Cancel' and check you get the expected response", function(done){
            const flow = testAdapter
                .send('Cancel')
                .assertReply(`Ok, let's start over.`);
            
            testNock.resolveWithMocks('cancelInterruption_response', done, flow);
        }); 
    });
});
