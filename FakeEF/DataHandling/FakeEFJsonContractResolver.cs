using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FakeEF.DataHandling
{
    internal class FakeEFJsonContractResolver : DefaultContractResolver
    {
        private readonly bool useProxy;
        private readonly List<string> includes;

        public FakeEFJsonContractResolver(bool useProxy, List<string> includes)
        {
            this.useProxy = useProxy;
            this.includes = includes;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            if (!useProxy)
            {
                properties = properties.Where(ShouldInclude).ToArray();
            }

            foreach (var prop in properties)
            {
                string name = prop?.PropertyName;
                if (name != null && type.IsClass)
                {
                    var propertyInfo = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (propertyInfo.GetSetMethod(true) != null)
                    {
                        prop.Writable = true;
                        prop.Ignored = false;
                    }
                }
            }

            return properties;
        }

        private bool ShouldInclude(JsonProperty jsonProperty)
        {
            if ((jsonProperty.PropertyType.IsClass || jsonProperty.PropertyType.IsInterface) && 
                jsonProperty.PropertyType != typeof(string))
            {
                if (includes.Any(y => y.Contains(jsonProperty.PropertyName)))
                    return true;
                return false;
            }
            return true;
        }
    }
}