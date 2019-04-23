// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const assert = require('assert');
const <%=skillTemplateName%> = require('./<%=skillTemplateNameFile%>');
const testNock = require('../testBase');
let testAdapter;

describe("sample dialog", function() {
    before(async function() {
        await <%=skillTemplateName%>.initialize();
        testAdapter = <%=skillTemplateName%>.getTestAdapter();
    });


    it("send 'Run Dialog' and check you get the expected response", function(done){

        const flow = testAdapter
            .send('run dialog')
            .assertReply('What is your name?');
        
        testNock.resolveWithMocks('sampleDialog_response', done, flow);
    }); 
});
