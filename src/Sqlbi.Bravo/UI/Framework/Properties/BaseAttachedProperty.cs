using System;
using System.Windows;

namespace Sqlbi.Bravo.UI.Framework.Properties
{
    internal abstract class BaseAttachedProperty<TParent, TProperty>
        where TParent : BaseAttachedProperty<TParent, TProperty>, new()
    {
        public event Action<DependencyObject, DependencyPropertyChangedEventArgs> ValueChanged;

        public event Action<DependencyObject, object> ValueUpdated;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
            name: "Value", 
            propertyType: typeof(TProperty), 
            ownerType: typeof(BaseAttachedProperty<TParent, TProperty>), 
            defaultMetadata: new UIPropertyMetadata(
                defaultValue: default(TProperty),
                propertyChangedCallback: new PropertyChangedCallback(OnValuePropertyChanged),
                coerceValueCallback: new CoerceValueCallback(OnValuePropertyUpdated)
                )
            );

        public static TParent Instance { get; private set; } = new TParent();

        private static void OnValuePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Instance.OnValueChanged(sender, e);
            Instance.ValueChanged?.Invoke(sender, e);
        }

        private static object OnValuePropertyUpdated(DependencyObject sender, object value)
        {
            Instance.OnValueUpdated(sender, value);
            Instance.ValueUpdated?.Invoke(sender, value);
            return value;
        }

        public static TProperty GetValue(DependencyObject d) => (TProperty)d.GetValue(ValueProperty);

        public static void SetValue(DependencyObject d, TProperty value) => d.SetValue(ValueProperty, value);

        public virtual void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }

        public virtual void OnValueUpdated(DependencyObject sender, object value)
        {
        }
    }
}
