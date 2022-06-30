using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using COLID.Common.DataModel.Attributes;
using COLID.Exception.Models.Business;
using COLID.RegistrationService.Common.Constants;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace COLID.RegistrationService.WebApi.Filters
{
    public class ValidateActionParametersAttribute : ActionFilterAttribute
    {
        /// <inheritdoc />
       public override void OnActionExecuting(ActionExecutingContext context)
        {
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (descriptor != null)
            {
                var parameters = descriptor.MethodInfo.GetParameters();

                CheckParameterRequired(context, parameters);
            }

            base.OnActionExecuting(context);
        }

        private static void CheckParameterRequired(ActionExecutingContext context, IEnumerable<ParameterInfo> parameters)
        {
            if (!context.ModelState.IsValid)
            {
                throw new RequestException(Messages.Request.Invalid);
            }
            
            var missingParamter = new List<string>();

            foreach (var parameter in parameters)
            {
                if (!context.ActionArguments.Keys.Contains(parameter.Name) && !parameter.CustomAttributes.Any(attr => attr.AttributeType == typeof(NotRequiredAttribute)))
                {
                    missingParamter.Add(parameter.Name);
                }
            }

            if (missingParamter.Any())
            {
                throw new MissingParameterException(Messages.Request.MissingParameter, missingParamter);
            }
        }

       
    }
}
