using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators
{
    internal class EntityPropertyValidator : IEntityPropertyValidator
    {
        private IEntityService _entityService;
        private IServiceProvider _serviceProvider;

        public EntityPropertyValidator(IEntityService entityService, IServiceProvider serviceProvider)
        {
            _entityService = entityService;
            _serviceProvider = serviceProvider;
        }

        public void Validate(string key, EntityValidationFacade validationFacade)
        {
            var validators = GetValidators();

            foreach (var validator in validators)
            {
                validator.HasValidationResult(validationFacade, GetKeyValuePair(key, validationFacade));
            }

            HandleOverwriteProperties(GetKeyValuePair(key, validationFacade), validationFacade);
        }

        private KeyValuePair<string, List<dynamic>> GetKeyValuePair(string key, EntityValidationFacade validationFacade)
        {
            if (!validationFacade.RequestResource.Properties.ContainsKey(key))
            {
                return new KeyValuePair<string, List<dynamic>>(key, new List<dynamic>());
            }

            return new KeyValuePair<string, List<dynamic>>(key, validationFacade.RequestResource.Properties[key]);
        }

        private IList<IEntityValidator> GetValidators()
        {
            var baseAssembly = typeof(IEntityValidator).Assembly;
            var typeList = baseAssembly.DefinedTypes
                .Where(type =>
                    !type.IsAbstract &&
                    type.ImplementedInterfaces.Any(imp => imp == typeof(IEntityValidator)))
                .ToList();

            IList<IEntityValidator> validators = typeList
                .Select(item => CreateInstance<IEntityValidator>(item))
                .OrderBy(x => x.Priority)
                .ToList();
            return validators;
        }

        private TType CreateInstance<TType>(TypeInfo typeInfo)
        {
            ConstructorInfo constructor = typeInfo.GetConstructors()?.FirstOrDefault();

            if (constructor != null)
            {
                object[] args = constructor
                    .GetParameters()
                    .Select(o => _serviceProvider.GetService(o.ParameterType))
                    .ToArray();

                return (TType)Activator.CreateInstance(typeInfo, args);
            }

            return (TType)Activator.CreateInstance(typeInfo);
        }

        private static void HandleOverwriteProperties(KeyValuePair<string, List<dynamic>> entityProperty, EntityValidationFacade validationFacade)
        {
            // If crud operation is update, then overwrite all specified properties from repo resource
            if (ResourceCrudAction.Update == validationFacade.ResourceCrudAction)
            {
                if (Common.Constants.Validation.OverwriteProperties.Contains(entityProperty.Key))
                {
                    var repoResource = validationFacade.ResourcesCTO.GetDraftOrPublishedVersion();
                    validationFacade.RequestResource.Properties[entityProperty.Key] = repoResource.Properties.GetValueOrNull(entityProperty.Key, false);
                }
            }
        }
    }
}
