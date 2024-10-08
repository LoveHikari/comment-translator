using System;
using System.ComponentModel;
using System.Resources;

namespace CommentTranslator.Util
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly ResourceManager _resourceManager;
        private readonly string _resourceKey;

        public LocalizedDisplayNameAttribute(string resourceKey, Type resourceType)
        {
            _resourceManager = new ResourceManager(resourceType);
            _resourceKey = resourceKey;
        }

        public override string DisplayName
        {
            get
            {
                string displayName = _resourceManager.GetString(_resourceKey);
                return !string.IsNullOrEmpty(displayName) ? displayName : $"[{_resourceKey}]";
            }
        }
    }
}