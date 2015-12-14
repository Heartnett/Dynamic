using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DynamicWrappers
{
    /// <summary>
    /// Used as a painless solution to the implementation of the System.ComponentModel.INotifyPropertyChanged interface.
    /// The wrapped object instance can be used a model for data binding.
    /// </summary>
    /// <typeparam name="T">The type to be wrapped and used as a model for data binding.</typeparam>
    [TypeDescriptionProvider(typeof(DynamicModelDescriptionProvider))]
    public class DynamicWrapper<T> : DynamicObject, IDynamicModel
    {
        #region Fields
        /// <summary>
        /// Holds the instance of the wrapped object.
        /// </summary>
        protected T _instance;

        /// <summary>
        /// Holds the type information of the wrapped object.
        /// </summary>
        protected Type _type = typeof(T);

        /// <summary>
        /// Used to determine if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Contains the names and values of the properties dynamically added during runtime.
        /// </summary>
        private Dictionary<string, object> _dynamicMembers;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the DynamicWrapper class.
        /// </summary>
        /// <param name="parameters">The parameters used to initialize an instance of the wrapped object. (Optional)</param>
        public DynamicWrapper(params object[] parameters)
        {
            _instance = (T)Activator.CreateInstance(_type, parameters);
        }

        /// <summary>
        /// Initializes a new instance of the DynamicWrapper class.
        /// </summary>
        /// <param name="instance">The instance of the object to wrap.</param>
        public DynamicWrapper(T instance)
        {
            if (_instance == null) throw new ArgumentNullException(nameof(instance), "The parameter cannot be null.");
            _instance = instance;
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when a property value has changed.
        /// </summary>
        public virtual event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Used to release the resources used by the class.
        /// </summary>
        /// <param name="disposing">The value used to determine if the object is being disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (!disposing) return;

            if (_instance is IDisposable) ((IDisposable)_instance).Dispose();
            _type = null;
            _disposed = true;
        }

        /// <summary>
        /// Returns property information for the supplied property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to retrieve the information for.</param>
        /// <returns>Property information for the supplied property name.</returns>
        protected PropertyInfo GetProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("The parameter cannot be null or empty.", nameof(propertyName));
            return _type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("The parameter cannot be null or empty.", nameof(propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Attempts to invoke a method on the wrapped object.
        /// </summary>
        /// <param name="name">The name of the method to invoke.</param>
        /// <param name="arguments">The arguments to use when attempting to invoke the method.</param>
        /// <param name="result">The value returned from the method that has been invoked.</param>
        /// <returns>True if the method was successfully invoked.</returns>
        protected bool TryInvoke(string name, object[] arguments, out object result)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("The parameter cannot be null or empty", nameof(name));

            var method = _type.GetMethod(name, arguments == null || arguments.Length == 0 ? Type.EmptyTypes : arguments.Select(item => item.GetType()).ToArray());
            if (method == null)
            {
                result = null;
                return false;
            }

            try
            {
                result = method.Invoke(_instance, arguments);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve a property value on the wrapped object, or on the wrapper itself.
        /// </summary>
        /// <param name="name">The name of the property value to retieve.</param>
        /// <param name="result">The value retrieved from the property.</param>
        /// <returns>True if the property value was successfully retrieved.</returns>
        protected bool TryGetMember(string name, out object result)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("The parameter cannot be null or empty.", nameof(name));

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

        /// <summary>
        /// Attempts to set a property value on the wrapped object, or on the wrapper itself.
        /// </summary>
        /// <param name="name">The name of the property value to set.</param>
        /// <param name="value">The value to set on the property.</param>
        /// <returns>True if the property value was successfully set.</returns>
        protected bool TrySetMember(string name, object value)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("The parameter cannot be null or empty.", nameof(name));

            var property = GetProperty(name);

            if (property == null)
            {
                if (_dynamicMembers == null) _dynamicMembers = new Dictionary<string, object>();

                if (_dynamicMembers.ContainsKey(name))
                {
                    _dynamicMembers[name] = value;
                }
                else
                {
                    _dynamicMembers.Add(name, value);
                }

                RaisePropertyChanged(name);
                return true;
            }

            if (!property.CanWrite) return false;

            property.SetValue(_instance, value);
            RaisePropertyChanged(property.Name);

            return true;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the wrapper instance as a dynamic type.
        /// </summary>
        /// <returns>The wrapper instance as a dynamic type.</returns>
        public dynamic AsDynamic() => this;

        /// <summary>
        /// Releases the resources used by the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a collection of member names, from both the wrapped object instance and the wrapper itself.
        /// </summary>
        /// <returns>A collection of member names, from both the wrapped object instance and the wrapper itself.</returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(item => item.Name)
                        .Union(_type.GetMethods().Select(item => item.Name))
                        .Union(_dynamicMembers?.Keys);
        }

        /// <summary>
        /// Attempts to retrieve the value of the supplied index on the wrapped object, or on the wrapper itself (value of a dynamically added property).
        /// </summary>
        /// <param name="binder">Get index binder.</param>
        /// <param name="indexes">Index values to retrieve.</param>
        /// <param name="result">The value returned from the index.</param>
        /// <returns>True if the index value was successfully retrieved.</returns>
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

        /// <summary>
        /// Attempts to set the value of the supplied index on the wrapped object, or on the wrapper itself (dynamically added property).
        /// </summary>
        /// <param name="binder">Set index binder.</param>
        /// <param name="indexes">Index values to set.</param>
        /// <param name="value">The value to set on the supplied index.</param>
        /// <returns>True if the index value was successfully set.</returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            object returnedResult = null;

            if(TryInvoke("set_Item", indexes.Union(new object[] { value }).ToArray(), out returnedResult))
            {
                return true;
            }

            if(indexes.Length == 1)
            {
                return TrySetMember(indexes[0].ToString(), value);
            }

            return false;
        }

        /// <summary>
        /// Attempts to convert the wrapped object instance to another type.
        /// </summary>
        /// <param name="binder">Convert binder.</param>
        /// <param name="result">The value returned from the conversion.</param>
        /// <returns>True if the wrapped object instance was successfully converted to another type.</returns>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            var method = _type.GetMethod($"op_{(binder.Explicit ? "Explicit" : "Implicit")}", BindingFlags.Public | BindingFlags.Static);
            if(method != null && method.ReturnType == binder.ReturnType)
            {
                try
                {
                    result = method.Invoke(null, new object[] { _instance });
                    return true;
                }
                catch { }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="binder">Not applicable.</param>
        /// <param name="indexes">Not applicable.</param>
        /// <returns>False.</returns>
        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes) => false;

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="binder">Not applicable.</param>
        /// <returns>False.</returns>
        public override bool TryDeleteMember(DeleteMemberBinder binder) => false;

        /// <summary>
        /// Will be implemented.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="arg"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            return base.TryBinaryOperation(binder, arg, out result);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="binder">Not applicable.</param>
        /// <returns>False.</returns>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Attempts to perform unary operation on the wrapped object, or on the wrapper itself.
        /// </summary>
        /// <param name="binder">Unary operation binder.</param>
        /// <param name="result">The resulting value of the unary operation.</param>
        /// <returns>True if the unary operation was successful.</returns>
        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result) => TryInvoke($"op_{binder.Operation}", new object[] { _instance }, out result);

        /// <summary>
        /// Attempts to invoke a memeber on the wrapped object, or on the wrapper itself.
        /// </summary>
        /// <param name="binder">Invoke member binder.</param>
        /// <param name="args">The arguments to use when attempting to invoke the member.</param>
        /// <param name="result">The value returned from member that has been invoked.</param>
        /// <returns>True if the member was successfully invoked.</returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) => TryInvoke(binder.Name, args, out result);
        
        /// <summary>
        /// Attempts to get a member value.
        /// </summary>
        /// <param name="binder">Get member binder.</param>
        /// <param name="result">The value retrieved from the member.</param>
        /// <returns>True if the the value was successfully retrieved from the member.</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result) => TryGetMember(binder.Name, out result);

        /// <summary>
        /// Attempts to set a member value.
        /// </summary>
        /// <param name="binder">Set member binder.</param>
        /// <param name="value">The value to assign to the member</param>
        /// <returns>True if the member was successfully set.</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value) => TrySetMember(binder.Name, value);

        /// <summary>
        /// Returns the wrapped object instance contained in the dynamic model.
        /// </summary>
        /// <returns>The wrapped object instance contained in the dynamic model.</returns>
        public object GetModelInstance() => _instance;
        #endregion
    }
}
