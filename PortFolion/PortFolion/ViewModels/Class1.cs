using LiveCharts;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

namespace PortFolion.ViewModels {
	public class GraphDataManager {
		public DateTime? CurrentDate { get; set; }
		public CommonNode CurrentNode { get; set; }

		public int TargetLevel { get; set; }
		public int? DisplayItemsCount { get; set; }
		public _DividePattern Divide { get; set; }
		public Period TimePeriod { get; set; }
		public _CalculationUnit InvestmentUnit { get; set; }

		public BrakeDownList BrakeDown { get; }
		public TransitionList Transition { get; }
		public IndexList Index { get; }
		public VolatilityList Volatility { get; }

		void RefreshBrakeDownList() {
			var cd = CurrentNode.NodeIndex().CurrentDepth;
			var ch = CurrentNode.Height();
			var tg = ch < TargetLevel ? ch : TargetLevel;
			var tgns = CurrentNode.Levelorder()
				.Where(a => a.NodeIndex().CurrentDepth == cd + tg)//ここにmarge
				.OrderBy(a => a.Amount);
			BrakeDown.Clear();
			foreach(var data in tgns) {
				BrakeDown.Add(
					new PieSeries() {
						Title = "",
						Values = new ChartValues<ObservableValue>() { new ObservableValue(1) },
						DataLabels = true,
					});
			}
		}
		IEnumerable<string> GetBrakeDownRanking() {
			return BrakeDown.AsEnumerable().Select(a => a.Title);
		}
		void RefreshHistoryList() {
			var b = RootCollection.GetNodeLine(CurrentNode.Path);
		}
	}
	public class BrakeDownList : SeriesCollection { }
	public class TransitionList : SeriesCollection { }
	public class IndexList : SeriesCollection { }
	public class VolatilityList : SeriesCollection { }
}
