using System.ComponentModel.DataAnnotations;

namespace COLID.Common.DataModel.Attributes
{
    public sealed class NotRequiredAttribute : ValidationAttribute
    {
        /// <summary>
        ///  Initializes a new instance of the NotRequiredAttribute class
        /// </summary>
        public NotRequiredAttribute() { }

        /// <summary>
        /// Gets or sets a value that indicates whether an empty string is allowed.
        /// Returns:
        ///     true if an empty string is allowed; otherwise, false. The default value is false.
        /// </summary>
        public bool AllowEmptyStrings { get; set; }

        /// <summary>
        /// Checks that the value of the required data field is not empty.
        /// </summary>
        /// <param name="value"> The data field value to validate.</param>
        /// <returns>true if validation is successful; otherwise, false.</returns>
        public override bool IsValid(object value) => true;
    }
}
