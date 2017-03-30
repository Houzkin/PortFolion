using Houzkin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PortFolion.Views.Converter {
	
	public class TryStringToNumberConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var v = value as string;
			if (string.IsNullOrEmpty(v)) return value;
			var rwv = ResultWithValue.Of<double>(double.TryParse, v)
				.TrueOrNot(
				o => o == 0 ? "0" : o.ToString("#,#.##"),
				x => value);
			return rwv;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
