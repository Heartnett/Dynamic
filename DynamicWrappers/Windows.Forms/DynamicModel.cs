using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DynamicWrappers.Windows.Forms
{
    [TypeDescriptionProvider(typeof(DynamicModelDescriptionProvider))]
    public sealed class DynamicModel<T> : DynamicWrapper<T>
    {
        #region Fields
        private Dictionary<Control, BindingInfo> _boundControls;
        #endregion

        #region Constructors
        public DynamicModel(params object[] parameters) : base(parameters)
        {
            PropertyChanged += _propertyChanged;
        }

        public DynamicModel(T instance) : base(instance)
        {
            PropertyChanged += _propertyChanged;
        }
        #endregion

        #region Event Handlers
        private void _propertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_boundControls == null) return;

            IEnumerable<KeyValuePair<Control, BindingInfo>> controls = _boundControls.Where(element => element.Value.ModelProperty == e.PropertyName);
            if (!controls.Any()) return;
            
            foreach(var kvp in controls)
            {
                var controlProperty = kvp.Key
                                         .GetType()
                                         .GetProperty(kvp.Value.TargetProperty, BindingFlags.Public | BindingFlags.Instance);

                controlProperty.SetValue(kvp.Key, Convert.ChangeType(GetProperty(kvp.Value.ModelProperty).GetValue(_instance), controlProperty.PropertyType));
            }            
        }
        #endregion

        #region Protected methods
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _boundControls = null;
                PropertyChanged -= _propertyChanged;
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Public Methods
        public void Databind(Control control, string controlProperty, string propertyName)
        {
            if (_boundControls == null) _boundControls = new Dictionary<Control, BindingInfo>();
            if (!_boundControls.ContainsKey(control)) _boundControls.Add(control, new BindingInfo());

            var item = _boundControls[control];
            if (item.TargetProperty == controlProperty) return;

            item.TargetProperty = controlProperty;
            item.ModelProperty = propertyName;
            RaisePropertyChanged(propertyName);
        }
        #endregion
    }
}
