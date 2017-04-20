using LiveCharts.Defaults;
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

namespace PortFolion.Views {
	/// <summary>
	/// GraphTab.xaml の相互作用ロジック
	/// </summary>
	public partial class GraphTab : UserControl {
		public GraphTab() {
			InitializeComponent();
			//this.DataContext = new PortFolion.ViewModels.GraphDataManager();
			this.DataContext = new ViewModels.GraphTabViewModel();
		}


		//private void Axis_RangeChanged(LiveCharts.Events.RangeChangedEventArgs eventArgs) {

		//}

		//private void PieChart_DataHover(object sender, LiveCharts.ChartPoint chartPoint) {
		//	var v = chartPoint.Instance;
		//	var t = v.GetType();
		//	var ts = v as ObservableValue;
		//}
	}
}
