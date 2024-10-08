using System;
using System.ComponentModel;
using System.Resources;

namespace CommentTranslator.Util
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly ResourceManager _resourceManager;
        private readonly string _resourceKey;

        public LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
        {
            _resourceManager = new ResourceManager(resourceType);
            _resourceKey = resourceKey;
        }

        public override string Description
        {
            get
            {
                string description = _resourceManager.GetString(_resourceKey);
                return !string.IsNullOrEmpty(description) ? description : $"[{_resourceKey}]";
            }
        }
    }
}