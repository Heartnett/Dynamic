using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DynamicWrappers
{
    [TypeDescriptionProvider(typeof(DynamicModelDescriptionProvider))]
    public class DynamicWrapper<T> : DynamicObject, IDynamicModel, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        protected T _instance;
        protected Type _type = typeof(T);
        private bool _disposed;
        private Dictionary<string, object> _dynamicMembers;
        #endregion

        #region Constructors
        public DynamicWrapper(params object[] parameters)
        {
            _instance = (T)Activator.CreateInstance(_type, parameters);
        }
        
        public DynamicWrapper(T instance)
        {
            _instance = instance;
        }
        #endregion

        #region Events
        public virtual event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Protected Methods
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (!disposing) return;

            if (_instance is IDisposable) ((IDisposable)_instance).Dispose();
            _type = null;
            _disposed = true;
        }

        protected PropertyInfo GetProperty(string propertyName) => _type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool TryInvoke(string name, object[] args, out object result)
        {
            var method = _type.GetMethod(name, args == null || args.Length == 0 ? Type.EmptyTypes : args.Select(item => item.GetType()).ToArray());
            if (method == null)
            {
                result = null;
                return false;
            }

            try
            {
                result = method.Invoke(_instance, args);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        protected bool TryGetMember(string name, out object result)
        {
            var property = GetProperty(name);

            if (property == null)
            {
                if (_dynamicMembers == null || !_dynamicMembers.ContainsKey(name))
                {
                    result = null;
                    return false;
                }
                else if (_dynamicMembers.ContainsKey(name))
                {
                    result = _dynamicMembers[name];
                    return true;
                }
            }

            if (!property.CanRead)
            {
                result = null;
                return false;
            }

            result = property.GetValue(_instance);
            return true;
        }
        #endregion

        #region Public Methods
        public dynamic AsDynamic() => this;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override IEnumerable<string> GetDynamicMemberNames() => _type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(item => item.Name).Union(_dynamicMembers?.Keys);

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            object returnedResult = null;

            if(TryInvoke("get_Item", indexes, out returnedResult))
            {
                result = returnedResult;
                return true;
            }

            if (indexes.Length == 1)
            {
                return TryGetMember(indexes[0].ToString(), out result);
            }

            result = null;
            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            return base.TrySetIndex(binder, indexes, value);
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return base.TryConvert(binder, out result);
        }

        public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
        {
            return base.TryCreateInstance(binder, args, out result);
        }

        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes) => false;

        public override bool TryDeleteMember(DeleteMemberBinder binder) => false;

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            return base.TryBinaryOperation(binder, arg, out result);
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            return base.TryInvoke(binder, args, out result);
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result) => TryInvoke($"op_{binder.Operation}", new object[] { _instance }, out result);

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) => TryInvoke(binder.Name, args, out result);
        
        public override bool TryGetMember(GetMemberBinder binder, out object result) => TryGetMember(binder.Name, out result);

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var property = GetProperty(binder.Name);

            if(property == null)
            {
                if(_dynamicMembers == null) _dynamicMembers = new Dictionary<string, object>();

                if(_dynamicMembers.ContainsKey(binder.Name))
                {
                    _dynamicMembers[binder.Name] = value;
                }
                else
                {
                    _dynamicMembers.Add(binder.Name, value);
                }

                RaisePropertyChanged(binder.Name);
                return true;
            }

            if (!property.CanWrite) return false;

            property.SetValue(_instance, value);
            RaisePropertyChanged(property.Name);

            return true;
        }

        public object GetModelInstance() => _instance;
        #endregion
    }
}
