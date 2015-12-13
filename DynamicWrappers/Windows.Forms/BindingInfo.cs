namespace DynamicWrappers.Windows.Forms
{
    public class BindingInfo
    {
        #region Constructors
        public BindingInfo() : this(string.Empty, string.Empty) { }

        public BindingInfo(string targetProperty, string modelProperty)
        {
            TargetProperty = targetProperty;
            ModelProperty = modelProperty;
        } 
        #endregion

        #region Properties
        public string ModelProperty { get; set; }
        public string TargetProperty { get; set; } 
        #endregion
    }
}
