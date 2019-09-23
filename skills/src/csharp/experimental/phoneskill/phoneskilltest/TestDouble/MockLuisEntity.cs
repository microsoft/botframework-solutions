namespace PhoneSkillTest.TestDouble
{
    /// <summary>
    /// Information about an entity to be returned by a MockLuisRecognizer.
    /// </summary>
    public class MockLuisEntity
    {
        /// <summary>
        /// Gets or sets the type of the entity.
        /// </summary>
        /// <value>
        /// The type of the entity.
        /// </value>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extracted query substring.
        /// </summary>
        /// <value>
        /// The extracted query substring.
        /// </value>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start index of the extracted substring in the query. (The end index is calculated automatically.)
        /// </summary>
        /// <value>
        /// The start index of the extracted substring in the query. (The end index is calculated automatically.)
        /// </value>
        public int StartIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the resolved value of the entity.
        /// </summary>
        /// <value>
        /// The resolved value of the entity.
        /// This may be null.
        /// </value>
        public string ResolvedValue { get; set; }
    }
}
