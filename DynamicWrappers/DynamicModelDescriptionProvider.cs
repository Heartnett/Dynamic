using System;
using System.ComponentModel;

namespace DynamicWrappers
{
    public class DynamicModelDescriptionProvider : TypeDescriptionProvider
    {
        private static readonly TypeDescriptionProvider _default = TypeDescriptor.GetProvider(typeof(DynamicWrapper<>));

        public DynamicModelDescriptionProvider() : base(_default) { }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            var defaultDescriptor = base.GetTypeDescriptor(objectType, instance);
            return instance == null ? defaultDescriptor : new DynamicModelTypeDescriptor(instance);
        }
    }
}
