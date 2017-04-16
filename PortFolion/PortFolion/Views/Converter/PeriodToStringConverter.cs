﻿using PortFolion.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PortFolion.Views.Converter {
	class PeriodToStringConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if(value is Period) {
				var prd = (Period)value;
				switch (prd) {
				case Period.Weekly:
					return "週";
				case Period.Monthly:
					return "月";
				case Period.Quarterly:
					return "四半期";
				case Period.Yearly:
					return "年";
				}
			}
			return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value is Period) return value;
			var str = value.ToString();
			switch (str) {
			case "Weekly":
				return Period.Weekly;
			case "Monthly":
				return Period.Monthly;
			case "Quarterly":
				return Period.Quarterly;
			case "Yearly":
				return Period.Yearly;
			default:
				throw new ArgumentException("引数を列挙体 Period に変換できませんでした。", "value");

			}
		}
	}
}
