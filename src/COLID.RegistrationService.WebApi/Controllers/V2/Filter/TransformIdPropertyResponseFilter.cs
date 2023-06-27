using System.Collections;
using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.WebApi.Controllers.V2.ContractResolver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace COLID.RegistrationService.WebApi.Controllers.V2.Filter
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TransformIdPropertyResponseFilter : ActionFilterAttribute
    {
        private JsonSerializerSettings _serializerSettings;

        /// <summary>
        /// 
        /// </summary>
        public TransformIdPropertyResponseFilter()
        {
            var propertyResolver = new PropertyRenameSerializerContractResolver();

            propertyResolver.RenameProperty(typeof(object), "Id", "Subject");
            propertyResolver.RenameProperty(typeof(Entity), "Id", "Subject");
            propertyResolver.RenameProperty(typeof(MetadataGraphConfigurationOverviewDTO), "Id", "Subject");
            propertyResolver.RenameProperty(typeof(HistoricResourceOverviewDTO), "Id", "Subject");
            propertyResolver.RenameProperty(typeof(BaseEntityResultCTO), "Id", "Subject");

            _serializerSettings = new JsonSerializerSettings();
            _serializerSettings.ContractResolver = propertyResolver;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            // Detect that the result is of type JsonResult
            if (context.Result is OkObjectResult)
            {
                var result = context.Result as OkObjectResult;
                result.Value = TransformFilterContextValue(result.Value);
            }
            else if (context.Result is CreatedResult)
            {
                var result = context.Result as CreatedResult;
                result.Value = TransformFilterContextValue(result.Value);
            }

            base.OnResultExecuting(context);
        }

        private object TransformFilterContextValue(object result)
        {
            if (result is IEnumerable)
            {
                var arr = new List<object>();
                foreach (var value in (IEnumerable)result)
                {
                    if (IsTransformableEntityType(value))
                    {
                        string outputS = JsonConvert.SerializeObject(value, _serializerSettings);
                        var output = JsonConvert.DeserializeObject(outputS);
                        arr.Add(output);
                    }
                }

                return arr;
            }
            else
            {
                string outputS = JsonConvert.SerializeObject(result, _serializerSettings);
                var output = JsonConvert.DeserializeObject(outputS);
                return output;
            }
        }

        private static bool IsTransformableEntityType(object value)
        {
            return value is object ||
                   value is Entity ||
                   value is MetadataGraphConfigurationOverviewDTO ||
                   value is HistoricResourceOverviewDTO ||
                   value is BaseEntityResultCTO;
        }
    }
}
