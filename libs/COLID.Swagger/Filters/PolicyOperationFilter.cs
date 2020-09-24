using System;
using System.Linq;
using COLID.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace COLID.RegistrationService.WebApi.Swagger.Filters
{
    public class PolicyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Policy names map to scopes
            var requiredScopes = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy)
                .Distinct();

            var authorizationRequirements =
                AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IAuthorizationRequirement).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .ToList();

            if (requiredScopes.Any())
            {
                foreach (var authType in authorizationRequirements)
                {
                    if (requiredScopes.Any(scope => scope == authType.Name))
                    {
                        operation.Description = $"<h3 style=\"color: red\"> Authorization: {authType.GetDescription()}</h3> <br> {operation.Description}";
                    }
                }
            }
        }
    }
}
