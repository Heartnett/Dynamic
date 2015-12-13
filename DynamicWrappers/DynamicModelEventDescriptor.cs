using System;
using System.ComponentModel;
using System.Reflection;

namespace DynamicWrappers
{
    public class DynamicModelEventDescriptor : EventDescriptor
    {
        #region Fields
        private readonly EventInfo _event;
        private readonly object _instance;
        #endregion

        #region Constructors
        public DynamicModelEventDescriptor(object instance, EventInfo @event): base(@event.Name, null)
        {
            _instance = instance;
            _event = @event;
        }
        #endregion

        #region Properties
        public override bool IsBrowsable => true;

        public override string Category => string.Empty;

        public override Type ComponentType => null;

        public override Type EventType => _event.EventHandlerType;

        public override bool IsMulticast => _event.IsMulticast;
        #endregion

        #region Public Methods
        public override void AddEventHandler(object component, Delegate value) => _event.AddEventHandler(_instance, value);

        public override void RemoveEventHandler(object component, Delegate value) => _event.RemoveEventHandler(_instance, value); 
        #endregion
    }
}
