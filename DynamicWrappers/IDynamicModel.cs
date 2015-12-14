using System;
using System.ComponentModel;

namespace DynamicWrappers
{
    /// <summary>
    /// Describes the functions a dynamic model object should implement.
    /// </summary>
    public interface IDynamicModel: IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Returns the object instance as a dynamic type.
        /// </summary>
        /// <returns>The object instance as a dynamic type.</returns>
        dynamic AsDynamic();

        /// <summary>
        /// Returns the wrapped object instance contained in the dynamic model.
        /// </summary>
        /// <returns>The wrapped object instance contained in the dynamic model.</returns>
        object GetModelInstance();
    }
}