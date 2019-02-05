using Luis;
using System;
using System.Collections.Generic;
using System.Text;

namespace PointOfInterestSkillTests.Flow.Fakes
{
    public class MockGeneralIntent : General
    {
        private string userInput;
        private Intent intent;
        private double score;

        public MockGeneralIntent(string userInput)
        {
            this.Entities = new General._Entities();
            this.userInput = userInput;

            this.intent = General.Intent.None;
            this.score = 0.9;
        }

        public override _Entities Entities { get; set; }

        public override (Intent intent, double score) TopIntent()
        {
            return (intent, score);
        }
    }
}
