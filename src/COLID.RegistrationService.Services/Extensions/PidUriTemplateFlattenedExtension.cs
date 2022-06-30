using System.Text.RegularExpressions;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;

namespace COLID.RegistrationService.Services.Extensions
{
    public static class PidUriTemplateFlattenedExtension
    {
        /// <summary>
        /// Creates the matching regex for the pid uri template
        /// </summary>
        /// <param name="pidUriTemplateFlattened">the flat pid uri template</param>
        /// <returns>Regex of pid uri template</returns>
        public static string GetRegex(this PidUriTemplateFlattened pidUriTemplateFlattened)
        {
            var prefix = pidUriTemplateFlattened.BaseUrl + pidUriTemplateFlattened.Route;

            if (pidUriTemplateFlattened.IdType == Common.Constants.PidUriTemplateIdType.Guid)
            {
                return "^" + prefix + $"{Common.Constants.Regex.Guid}{pidUriTemplateFlattened.Suffix}$";
            }
            else if (pidUriTemplateFlattened.IdType == Common.Constants.PidUriTemplateIdType.Number)
            {
                return $"^{prefix}(\\d+){pidUriTemplateFlattened.Suffix}$";
            }
            else
            {
                throw new System.Exception($"Unrecognized id type {pidUriTemplateFlattened.IdType}.");
            }
        }

        /// <summary>
        /// Checks if the given string was generated from the pid uri template.
        /// </summary>
        /// <param name="pidUriTemplateFlattend">the flat pid uri template</param>
        /// <param name="str">String to be checked</param>
        /// <returns>true if matched, otherwise false</returns>
        public static bool IsMatch(this PidUriTemplateFlattened pidUriTemplateFlattened, string str)
        {
            var reg = GetRegex(pidUriTemplateFlattened);

            return Regex.IsMatch(str, reg);
        }
    }
}
