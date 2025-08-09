using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Converters;

internal class BoolToOpacityConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(
        nameof(TrueValue),
        typeof(double),
        typeof(BoolToOpacityConverter),
        new PropertyMetadata(1.0));

    public double TrueValue
    {
        get { return (double)GetValue(TrueValueProperty); }
        set { SetValue(TrueValueProperty, value); }
    }

    public static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(
        nameof(FalseValue),
        typeof(double),
        typeof(BoolToOpacityConverter),
        new PropertyMetadata(0.25));

    public double FalseValue
    {
        get { return (double)GetValue(FalseValueProperty); }
        set { SetValue(FalseValueProperty, value); }
    }

    public static readonly DependencyProperty NullValueProperty = DependencyProperty.Register(
        nameof(NullValue),
        typeof(double),
        typeof(BoolToOpacityConverter),
        new PropertyMetadata(0.25));

    public double NullValue
    {
        get { return (double)GetValue(NullValueProperty); }
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
