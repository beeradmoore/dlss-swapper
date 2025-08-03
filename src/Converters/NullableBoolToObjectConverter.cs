using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Converters;

internal class NullableBoolToObjectConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(
        nameof(TrueValue),
        typeof(object),
        typeof(NullableBoolToObjectConverter),
        new PropertyMetadata(null));

    public object TrueValue
    {
        get { return (object)GetValue(TrueValueProperty); }
        set { SetValue(TrueValueProperty, value); }
    }

    public static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(
        nameof(FalseValue),
        typeof(object),
        typeof(NullableBoolToObjectConverter),
        new PropertyMetadata(null));

    public object FalseValue
    {
        get { return (object)GetValue(FalseValueProperty); }
        set { SetValue(FalseValueProperty, value); }
    }

    public static readonly DependencyProperty NullValueProperty = DependencyProperty.Register(
        nameof(NullValue),
        typeof(object),
        typeof(NullableBoolToObjectConverter),
        new PropertyMetadata(null));

    public object NullValue
    {
        get { return (object)GetValue(NullValueProperty); }
        set { SetValue(NullValueProperty, value); }
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueValue : FalseValue;
        }

        return NullValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
