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
		IEnumerable<ViewModels.SeriesValue> tryGetVm() {
			var p = this.Parent as FrameworkElement;
			while(p != null) {
				var dc = p as PieChart;
				if(dc == null) {
					p = p.Parent as FrameworkElement;
					continue;
				}else {
					return dc.Series
						.Select(a => a.Values.OfType<ViewModels.SeriesValue>())
						.SelectMany(a => a);
				}
			}
			return Enumerable.Empty<ViewModels.SeriesValue>();
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
		
		public static readonly DependencyProperty BulletSizeProperty //= DefaultLegend.BulletSizeProperty.AddOwner(typeof(PieChartCustomLegend),new FrameworkPropertyMetadata(typeof(PieChartCustomLegend)));
			= DependencyProperty.Register(
			"BulletSize", typeof(double), typeof(PieChartCustomLegend), new PropertyMetadata(15d));

		public double BulletSize {
            get { return (double)GetValue(BulletSizeProperty); }
            set { SetValue(BulletSizeProperty, value); }
		}

		/// <summary>
		/// The orientation property
		/// </summary>
		public static readonly DependencyProperty OrientationProperty //= DefaultLegend.OrientationProperty.AddOwner(typeof(PieChartCustomLegend), new FrameworkPropertyMetadata(typeof(PieChartCustomLegend)));
		 = DependencyProperty.Register(
		"Orientation", typeof(Orientation?), typeof(PieChartCustomLegend), new PropertyMetadata(null));
		/// <summary>
		/// Gets or sets the orientation of the legend, default is null, if null LiveCharts will decide which orientation to use, based on the Chart.Legend location property.
		/// </summary>
		public Orientation? Orientation {
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		/// <summary>
		/// The internal orientation property
		/// </summary>
		public static readonly DependencyProperty InternalOrientationProperty //= DefaultLegend.InternalOrientationProperty.AddOwner(typeof(PieChartCustomLegend), new FrameworkPropertyMetadata(typeof(PieChartCustomLegend)));
			= DependencyProperty.Register(
			"InternalOrientation", typeof(Orientation), typeof(PieChartCustomLegend),
			new PropertyMetadata(default(Orientation)));

		/// <summary>
		/// Gets or sets the internal orientation.
		/// </summary>
		/// <value>
		/// The internal orientation.
		/// </value>
		public Orientation InternalOrientation {
			get { return (Orientation)GetValue(InternalOrientationProperty); }
			set { SetValue(InternalOrientationProperty, value); }
		}

	}
}
