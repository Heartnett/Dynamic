using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DynamicWrappers
{
    public class DynamicModelTypeDescriptor : ICustomTypeDescriptor
    {
        #region Fields
        private readonly IDynamicModel _instance;
        private readonly Type _modelType;
        private BindingFlags _publicInstanceBinding = BindingFlags.Instance | BindingFlags.Public;
        #endregion

        #region Constructors
        public DynamicModelTypeDescriptor(object instance)
        {
            _instance = (IDynamicModel)instance;
            _modelType = _instance.GetModelInstance().GetType();
        }
        #endregion

        #region Public Methods
        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);

        public string GetClassName() => TypeDescriptor.GetClassName(this, true);

        public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);

        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);

        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

        public PropertyDescriptor GetDefaultProperty() => null;

        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

        public EventDescriptorCollection GetEvents() => GetEvents(new Attribute[0]);

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            var model = _instance.GetModelInstance();
            return new EventDescriptorCollection(_modelType.GetType()
                                                           .GetEvents(_publicInstanceBinding)
                                                           .Select(item => new DynamicModelEventDescriptor(model, item))
                                                           .Union(_instance.GetType()
                                                                           .GetEvents(_publicInstanceBinding)
                                                                           .Select(item => new DynamicModelEventDescriptor(_instance, item)))
                                                           .ToArray());
        }

        public PropertyDescriptorCollection GetProperties() => GetProperties(new Attribute[0]);

        public PropertyDescriptorCollection GetProperties(Attribute[] attibutes)
        {
            return new PropertyDescriptorCollection(_modelType.GetProperties(_publicInstanceBinding)
                                                              .Select(item => new DynamicModelPropertyDescriptor(_instance, item))
                                                              .ToArray());
        }

        public object GetPropertyOwner(PropertyDescriptor pd) => _instance; 
        #endregion
    }
}
