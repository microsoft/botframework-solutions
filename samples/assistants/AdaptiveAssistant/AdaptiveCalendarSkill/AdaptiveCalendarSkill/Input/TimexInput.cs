using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json.Linq;
using Constants = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants;
using System;
using System.Linq;

namespace AdaptiveCalendarSkill.Input
{
    public class TimexInput : InputDialog
    {
        public string DefaultLocale { get; set; } = null;

        public string TimexType { get; set; } = null;

        protected override Task<InputState> OnRecognizeInput(DialogContext dc)
        {
            var input = dc.State.GetValue<object>(VALUE_PROPERTY);

            var culture = GetCulture(dc);
            var results = DateTimeRecognizer.RecognizeDateTime(input.ToString(), culture);
            if (results.Count > 0)
            {
                var result = new Dictionary<string, JArray>();

                // If we're missing the requested timex type, throw invalid and reprompt.
                if (TimexType != null)
                {
                    // if we have a specific timexType, we should check if its available, if not, throw invalid
                    var exists = results.Any(r => r.TypeName.Contains(TimexType));

                    if (!exists)
                    {
                        return Task.FromResult(InputState.Invalid);
                    }
                }

                foreach (var res in results)
                {
                    var resolutionValues = (List<Dictionary<string, string>>)res.Resolution["values"];
                    var type = resolutionValues[0]["type"];
                    var timexValues = new JArray();

                    foreach (var timex in resolutionValues)
                    {
                        if (type == Constants.TimexTypes.DateTimeRange)
                        {
                            var dateRange = TimexHelpers.DateRangeFromTimex(new TimexProperty(timex["timex"]));
                            dynamic dateRangeObj = new JObject();
                            dateRangeObj.start = dateRange.Start;
                            dateRangeObj.end = dateRange.End;
                            timexValues.Add(dateRangeObj);
                        }
                        else if (type == Constants.TimexTypes.DateRange)
                        {
                            var dateRange = TimexHelpers.DateRangeFromTimex(new TimexProperty(timex["timex"]));
                            dynamic dateRangeObj = new JObject();
                            dateRangeObj.start = dateRange.Start;
                            dateRangeObj.end = dateRange.End;
                            timexValues.Add(dateRangeObj);
                        }
                        else if (type == Constants.TimexTypes.TimeRange)
                        {
                            var timeRange = TimexHelpers.TimeRangeFromTimex(new TimexProperty(timex["timex"]));
                            dynamic timeRangeObj = new JObject();
                            timeRangeObj.start = timeRange.Start;
                            timeRangeObj.end = timeRange.End;
                            timexValues.Add(timeRangeObj);
                        }
                        else if (type == Constants.TimexTypes.DateTime)
                        {
                            var dateTime = TimexHelpers.DateFromTimex(new TimexProperty(timex["timex"]));
                            timexValues.Add(JToken.FromObject(dateTime));
                        }
                        else if (type == Constants.TimexTypes.Date)
                        {
                            var resolution = TimexResolver.Resolve(new[] { timex["timex"] }, DateTime.Today);
                            foreach(var val in resolution.Values)
                            {
                                var date = DateTime.Parse(val.Value);
                                if (!timexValues.Contains(JToken.FromObject(date)))
                                {
                                    timexValues.Add(JToken.FromObject(date));
                                }
                            }
                        }
                        else if (type == Constants.TimexTypes.Time)
                        {
                            var time = TimexHelpers.TimeFromTimex(new TimexProperty(timex["timex"]));
                            timexValues.Add(JToken.FromObject(time));
                        }
                        else if (type == Constants.TimexTypes.Duration)
                        {
                            timexValues.Add(timex["value"]);
                        }
                    }

                    result[type] = timexValues;
                }

                // Save timex result
                dc.State.SetValue(VALUE_PROPERTY, result);
            }
            else
            {
                return Task.FromResult(InputState.Unrecognized);
            }

            return Task.FromResult(InputState.Valid);
        }

        private string GetCulture(DialogContext dc)
        {
            if (!string.IsNullOrEmpty(dc.Context.Activity.Locale))
            {
                return dc.Context.Activity.Locale;
            }

            if (!string.IsNullOrEmpty(this.DefaultLocale))
            {
                return this.DefaultLocale;
            }

            return Culture.English;
        }
    }
}
