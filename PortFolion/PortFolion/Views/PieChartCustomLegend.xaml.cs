using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace PortFolion.Views {
	public class PieSeriesCustomViewModel : SeriesViewModel {
		public double Participation { get; set; }
	}
	/// <summary>
	/// PieChartCustomLegend.xaml の相互作用ロジック
	/// </summary>
	public partial class PieChartCustomLegend : UserControl, IChartLegend {
		public PieChartCustomLegend() {
			InitializeComponent();
			DataContext = this;

		}
		IEnumerable<ViewModels.TempValue> tryGetVm() {
			var p = this.Parent as FrameworkElement;
			while(p != null) {
				var dc = p as PieChart;
				if(dc == null) {
					p = p.Parent as FrameworkElement;
					continue;
				}else {
					return dc.Series
						.Select(a => a.Values.OfType<ViewModels.TempValue>())
						.SelectMany(a => a);
				}
			}
			return Enumerable.Empty<ViewModels.TempValue>();
		}
		List<SeriesViewModel> _series;
		public List<SeriesViewModel> Series {
			get { return _series; }
			set {
				var am = tryGetVm();
				if (am.Any()) {
					_series = value.Zip(am, (v, a) => new PieSeriesCustomViewModel() {
						Fill = v.Fill,
						PointGeometry = v.PointGeometry,
						Stroke = v.Stroke,
						Title = v.Title,
						StrokeThickness = v.StrokeThickness,
						Participation = a.Rate,
					}).ToList<SeriesViewModel>();
				}else {
					_series = value;
				}
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Series"));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public static readonly DependencyProperty BulletSizeProperty = DependencyProperty.Register(
			"BulletSize", typeof(double), typeof(PieChartCustomLegend), new PropertyMetadata(15d));
        
        public double BulletSize {
            get { return (double)GetValue(BulletSizeProperty); }
            set { SetValue(BulletSizeProperty, value); }
		}
	}
}
