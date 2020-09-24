using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace COLID.RegistrationService.WebApi.Swagger.Filters
{
    public class ReplaceVersionWithExactValueInPathFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var newOpenApiPaths = new OpenApiPaths();

            foreach (var path in swaggerDoc.Paths)
            {
                newOpenApiPaths.Add(path.Key.Replace("v{version}", swaggerDoc.Info.Version), path.Value);
            }

            swaggerDoc.Paths = newOpenApiPaths;
        }
    }
}
