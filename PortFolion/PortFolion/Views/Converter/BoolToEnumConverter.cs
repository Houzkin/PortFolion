using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PortFolion.Views.Converter {
    /// <summary>
    /// 列挙対とBool値のコンバータ。ラジオボタンと列挙体を繋げる。
    /// </summary>
    class BoolToEnumConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var prm = parameter as string;
            if (prm == null)
                return System.Windows.DependencyProperty.UnsetValue;
            if (Enum.IsDefined(value.GetType(), value) == false)
                return System.Windows.DependencyProperty.UnsetValue;
            object paramvalue = Enum.Parse(value.GetType(), prm);
            return (int)paramvalue == (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var prm = parameter as string;
            return prm == null ? System.Windows.DependencyProperty.UnsetValue : Enum.Parse(targetType, prm);
        }
    }
}
