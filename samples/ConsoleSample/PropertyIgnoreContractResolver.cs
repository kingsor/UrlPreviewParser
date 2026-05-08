using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace ConsoleSample
{
    internal class PropertyIgnoreContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<Type, HashSet<string>> _ignores;

        public PropertyIgnoreContractResolver()
        {
            _ignores = new Dictionary<Type, HashSet<string>>();
        }

        public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
        {
            if (!_ignores.ContainsKey(type))
                _ignores[type] = new HashSet<string>();

            foreach (var prop in jsonPropertyNames)
                _ignores[type].Add(prop);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property is not null && IsIgnored(property.DeclaringType, property.PropertyName))
            {
                property.ShouldSerialize = i => false;
                property.Ignored = true;
            }

            return property;
        }

        private bool IsIgnored(Type type, string jsonPropertyName)
        {
            return !_ignores.ContainsKey(type) ? false : _ignores[type].Contains(jsonPropertyName);
        }
    }
}
