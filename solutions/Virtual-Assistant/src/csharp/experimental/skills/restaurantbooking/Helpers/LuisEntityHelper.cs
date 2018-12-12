namespace RestaurantBooking.Helpers
{
    using System;
    using System.Linq;
    using Microsoft.Bot.Builder.AI.Luis;
    using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class LuisEntityHelper
    {
        /// <summary>
        /// Returns the normalized value for an list entity.
        /// </summary>
        /// <param name="entity">Entity to process.</param>
        /// <returns>Normalized value.</returns>
        public static string TryGetNormalizedValueFromListEntity(JToken entity)
        {
            if (entity != null && entity.HasValues && entity[0].HasValues)
            {
                return (string)entity[0][0];
            }

            return null;
        }

        /// <summary>
        /// Gets a Date from builtin DateTime entity (it is up to the caller to ignore the time portion of the result).
        /// </summary>
        /// <param name="entity">Entity to process.</param>
        /// <param name="futureOnly">Only interested in the future.</param>
        /// <returns>DateTime.</returns>
        public static DateTime? TryGetDateFromEntity(JToken entity, bool futureOnly = false)
        {
            DateTime? retDate = null;
            if (entity?.Count() > 0)
            {
                var dateTimeSpec = JsonConvert.DeserializeObject<DateTimeSpec>(JsonConvert.SerializeObject(entity[0]));
                var resolution = TimexResolver.Resolve(new[] { dateTimeSpec.Expressions[0] }, DateTime.UtcNow.Date);
                var value = futureOnly ? resolution.Values[resolution.Values.Count - 1] : resolution.Values[0];
                switch (dateTimeSpec.Type)
                {
                    case "date":
                        retDate = DateTime.Parse(value.Value);
                        break;
                    case "datetimerange":
                        if (value.Type == "datetimerange")
                        {
                            // Hack for now we replace 24:00:00 by 23:59:59 so we can parse it.
                            retDate = DateTime.Parse(value.End.Replace("24:00:00", "23:59:59"));
                        }

                        break;
                }
            }

            return retDate;
        }

        /// <summary>
        /// Gets a Time from builtin DateTime entity (it is up to the caller to ignore the Date portion of the result).
        /// </summary>
        /// <param name="entity">Entity to process.</param>
        /// <param name="futureOnly">Only interested in the future.</param>
        /// <returns>DateTime.</returns>
        public static DateTime? TryGetTimeFromEntity(JToken entity, bool futureOnly = false)
        {
            DateTime? retDate = null;
            if (entity?.Count() > 0)
            {
                var sp = JsonConvert.DeserializeObject<DateTimeSpec>(JsonConvert.SerializeObject(entity[0]));
                switch (sp.Type)
                {
                    case "time":
                        var r = TimexResolver.Resolve(new[] { sp.Expressions[0] }, DateTime.UtcNow.Date);
                        var value = futureOnly ? r.Values[r.Values.Count - 1] : r.Values[0];
                        retDate = DateTime.Parse(value.Value);
                        break;
                    case "datetime":
                        if (sp.Expressions?.Count > 0 && sp.Expressions[0] == "PRESENT_REF")
                        {
                            retDate = DateTime.UtcNow;
                        }
                        else
                        {
                            var resolvedDateTime = TimexResolver.Resolve(new[] { sp.Expressions[0] }, DateTime.UtcNow.Date);
                            var dateTimeValue = futureOnly ? resolvedDateTime.Values[resolvedDateTime.Values.Count - 1] : resolvedDateTime.Values[0];
                            retDate = DateTime.Parse(dateTimeValue.Value);
                        }

                        break;
                }
            }

            return retDate;
        }

        /// <summary>
        /// Gets a DateTime from builtin DateTime entity.
        /// </summary>
        /// <param name="entity">Entity to process.</param>
        /// <param name="futureOnly">Only interested in the future.</param>
        /// <returns>DateTime.</returns>
        public static DateTime? TryGetDateTimeFromEntity(JToken entity, bool futureOnly = false)
        {
            DateTime? retDate = null;
            if (entity?.Count() > 0)
            {
                var sp = JsonConvert.DeserializeObject<DateTimeSpec>(JsonConvert.SerializeObject(entity[0]));
                switch (sp.Type)
                {
                    case "datetime":
                        if (sp.Expressions?.Count > 0)
                        {
                            if (sp.Expressions[0] == "PRESENT_REF")
                            {
                                retDate = DateTime.UtcNow;
                            }
                            else
                            {
                                var r = TimexResolver.Resolve(new[] { sp.Expressions[0] }, DateTime.UtcNow.Date);
                                var value = futureOnly ? r.Values[r.Values.Count - 1] : r.Values[0];
                                retDate = DateTime.Parse(value.Value);
                            }
                        }
                        else
                        {
                            // Just in case we don't find an expression we haven't considered.
                            throw new ApplicationException($"Don't know how to resolve LUIS expression {sp}");
                        }

                        break;
                }
            }

            return retDate;
        }

        /// <summary>
        /// Gets the value from a simple LUIS entity.
        /// </summary>
        /// <param name="entity">Entity to process.</param>
        /// <returns>Value.</returns>
        public static string TryGetValueFromEntity(JToken entity)
        {
            if (entity != null && entity.HasValues)
            {
                return (string)entity[0];
            }

            return null;
        }
    }
}
