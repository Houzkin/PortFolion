using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Houzkin;

namespace PortFolion.Views.Converter {
	public class PathToStringConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var v = value as IEnumerable<string>;
			if (v == null || !v.Any()) return "-";
			return v.Aggregate((ac, s) => ac + "/" + s);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
