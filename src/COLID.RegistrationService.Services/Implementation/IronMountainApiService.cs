using COLID.IronMountainService.Common.Models;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace COLID.RegistrationService.Services.Implementation
{
    public class IronMountainApiService : IIronMountainApiService
    {
        private readonly ILogger<IronMountainApiService> _logger;
        private readonly IIronMountainRepository _ironMountainRepository;

        public IronMountainApiService(ILogger<IronMountainApiService> logger, IIronMountainRepository ironMountainRepository)
        {
            _logger = logger;
            _ironMountainRepository = ironMountainRepository;
        }

        public async Task<IronMountainRentionScheduleDto> GetAllRecordClasses()
        {
            var allRecordClassesObject = new IronMountainRentionScheduleDto();
            try
            {
                allRecordClassesObject = await _ironMountainRepository.GetIronMountainData();
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError("An error occured to authenticate to iron mountain", ex);
            }
            return allRecordClassesObject;
        }

        public async Task<List<IronMountainResponseDto>> GetResourcePolicies(ISet<IronMountainRequestDto> policyRequestValues)
        {
            var retentionScheduleResponse = new List<IronMountainResponseDto>();
            try
            {
                IronMountainRentionScheduleDto retentionSchedule = await GetAllRecordClasses();
                foreach (var policyRequest in policyRequestValues)
                {
                    List<RetentionClassPolicies> retentionClassPolicies = new List<RetentionClassPolicies>();
                    foreach (var category in policyRequest.dataCategories)
                    {
                    var dataCategoryId = category.Substring(category.LastIndexOf('/') + 1);
                    var recordClass =  GetRecordClassesByHierarchy(retentionSchedule.retentionSchedule, dataCategoryId).FirstOrDefault();
                    retentionClassPolicies = recordClass != null ? mapRecordClasses(recordClass, retentionClassPolicies) : retentionClassPolicies;
                    }
                    IronMountainResponseDto ironMountainResponseDto = new IronMountainResponseDto();
                    ironMountainResponseDto.pidUri = policyRequest.pidUri;
                    ironMountainResponseDto.retentionClassPolicies = retentionClassPolicies;
                    retentionScheduleResponse.Add(ironMountainResponseDto); 
                }

            }
            catch (SystemException ex)
            {
                _logger.LogError("Could not fetch retention schedule from IronMountain.", ex);
            }
            return retentionScheduleResponse;
        }

        private List<Policy> mapPolicyRules(List<IronMountainRecordClassRules> rules)
        {
            List<Policy> policiesList = new List<Policy>();
            foreach (var rule in rules ?? Enumerable.Empty<IronMountainRecordClassRules>())
            {
                Policy policy = new Policy();
                policy.url = null;
                policy.ruleName = rule.ruleName;
                policy.jurisdiction = rule.jurisdiction;
                policy.retentionTriggerId = rule.retentionTriggerId;
                policy.retentionTrigger = rule.retentionTrigger;
                policy.retentionTriggerDescription = rule.retentionTriggerDescription;
                policy.minRetentionPeriod = rule.minRetentionPeriod;
                policy.minRetentionPeriodUnits = rule.minRetentionPeriodUnits;
                policy.maxRetentionPeriod = rule.maxRetentionPeriod;
                policy.maxRetentionPeriodUnits = rule.maxRetentionPeriodUnits;
                policiesList.Add(policy);
            }
            return policiesList;
        }


        private List<RetentionClassPolicies> mapRecordClasses(IronMountainRecordClass recordClass, List<RetentionClassPolicies> retentionClassPolicies)
        {
            RetentionClassPolicies retentionClass = new RetentionClassPolicies();

            retentionClass.classId = recordClass.recordClassId;
            retentionClass.className = recordClass.recordClassName;
            retentionClass.classDescription = recordClass.recordClassDescription;
            retentionClass.policies = mapPolicyRules(recordClass.rules);
            retentionClassPolicies.Add(retentionClass);
            if (recordClass.children != null && recordClass.children.Count > 0)
            {
                foreach (var child in recordClass.children)
                {
                    RetentionClassPolicies retentionClassChild = new RetentionClassPolicies();
                    retentionClassChild.classId = child.recordClassId;
                    retentionClassChild.className = child.recordClassName;
                    retentionClassChild.classDescription = child.recordClassDescription;
                    retentionClassChild.policies = mapPolicyRules(child.rules);
                    retentionClassPolicies.Add(retentionClassChild); 
                }
            }
            return retentionClassPolicies; 
        }

        private static IEnumerable<IronMountainRecordClass> GetRecordClassesByHierarchy(IEnumerable<IronMountainRecordClass> retentionSchedule, string dataCategoryId)
        {
            if (retentionSchedule.Any(x => x.recordClassId == dataCategoryId))
            {
                return retentionSchedule.Where(x => x.recordClassId == dataCategoryId);
            }
            else
            {
                var checkChildRecordClass = retentionSchedule.Where(x => x.children != null && x.children.Count > 0).SelectMany(x => x.children);
                if (checkChildRecordClass.Count() > 0)
                {
                    return GetRecordClassesByHierarchy(checkChildRecordClass, dataCategoryId);
                }
            }
            return new List<IronMountainRecordClass>();
        }
    }
}
