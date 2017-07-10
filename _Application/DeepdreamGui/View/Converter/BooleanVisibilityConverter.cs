using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DeepdreamGui.View.Converter
{
    /// <summary>
    /// UI Converter for Bool to Visibility (true:Visible, false:Collapsed)
    /// </summary>
    [ValueConversion(typeof (bool), typeof(Visibility))]
    public class BooleanVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert Boolean to Visibility
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>true: Visible, false: Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (!(value is bool)) throw new Exception("Value is not boolean");
            return  (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Convert Visibility to Boolean
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Visible:true, false</returns>
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (!(value is Visibility)) throw new Exception("Value is not Visibility");
            return (Visibility) value == Visibility.Visible;
        }
    }
}
