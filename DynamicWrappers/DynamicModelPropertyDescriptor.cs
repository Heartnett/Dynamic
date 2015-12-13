using System;
using System.ComponentModel;
using System.Reflection;

namespace DynamicWrappers
{
    public class DynamicModelPropertyDescriptor : PropertyDescriptor
    {
        #region Fields
        private readonly IDynamicModel _instance;
        private readonly object _model;
        private readonly PropertyInfo _property;
        #endregion

        #region Constructors
        public DynamicModelPropertyDescriptor(IDynamicModel instance, PropertyInfo property) : base(property.Name, null)
        {
            _instance = instance;
            _model = _instance.GetModelInstance();
            _property = property;
        }
        #endregion

        #region Properties
        public override Type ComponentType => null;

        public override string Category => string.Empty;

        public override string Description => string.Empty;

        public override bool IsReadOnly => !_property.CanWrite;

        public override Type PropertyType => _property.PropertyType;
        
        public override bool SupportsChangeEvents => true;

        public override bool IsBrowsable => true;
        #endregion

        #region Public Methods
        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => _property.GetValue(_model);

        public override void ResetValue(object component) { }

        public override void SetValue(object component, object value) => _property.SetValue(_model, value);

        public override bool ShouldSerializeValue(object component) => true;
        #endregion

        #region Protected Methods
        protected override void OnValueChanged(object component, EventArgs e)
        {
            _instance.GetType()
                     .GetMethod("RaisePropertyChange", BindingFlags.Instance | BindingFlags.NonPublic)
                     .Invoke(_instance, new object[] {  });
        }
        #endregion
    }
}
