using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace COLID.RegistrationService.Common.Extensions
{
    public static class EnumExtension
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return null;
            var attribute = (DescriptionAttribute)fieldInfo.GetCustomAttribute(typeof(DescriptionAttribute));
            return attribute.Description;
        }

        public static string GetEnumMember(this Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return null;
            var attribute = (EnumMemberAttribute)fieldInfo.GetCustomAttribute(typeof(EnumMemberAttribute));
            return attribute.Value;
        }

        public static T GetValueFromEnumMember<T>(string enumMember)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                    typeof(EnumMemberAttribute)) is EnumMemberAttribute attribute)
                {
                    if (attribute.Value == enumMember)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == enumMember)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", nameof(enumMember));
            // or return default(T);
        }
    }
}
