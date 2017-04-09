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
using Livet;
using Houzkin.Architecture;
using PortFolion.IO;
using System.Collections.ObjectModel;

namespace PortFolion.ViewModels {
	public enum Period {
		Yearly,
		Quarterly,
		Monthly,
		Weekly,
	}
	public enum DividePattern {
		Tag,
		Location,
	}
	public enum TransitionStatus {
		BalanceOnly,
		SingleCashFlow,
		StackCashFlow,
		ProfitLossOnly,
	}
	//public enum VolatilityType {
	//	Normal,
	//	Log,
	//}
	public class TempValue {
		public string Title { get; set; }
		public double Amount { get; set; }
		public double Rate { get; set; }
		//public double Invest { get; set; }
		//public NodeType Type { get; set; }
	}
	public class GraphValue {
		/// <summary>残高</summary>
		public double Amount { get; set; }
		/// <summary>外部キャッシュフロー</summary>
		public double Flow { get; set; }
		/// <summary>修正ディーツ法による変動比率</summary>
		public double Dietz { get; set; } = 1;
		public DateTime Date { get; set; }
	}
	public class DateSpan {
		public DateSpan(DateTime date1, DateTime date2) {
			if (date1 <= date2) {
				Start = date1;
				End = date2;
			} else {
				Start = date2;
				End = date1;
			}
		}
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}
	public static class Ext {
		public static IEnumerable<T> Dequeue<T>(this Queue<T> self,Func<T,bool> pred) {
			if (!self.Any()) return Enumerable.Empty<T>();
			var lst = new List<T>();
			while (self.Any() && pred(self.Peek())) {
				lst.Add(self.Dequeue());
			}
			return lst;
		}
		
		public static IEnumerable<GraphValue> ToGraphValues(this Dictionary<DateTime,CommonNode> src, Period period) {
			if (src == null || !src.Any()) return Enumerable.Empty<GraphValue>();
			var ax = Ext.GetTimeAxis(period, src.Keys.Min(), src.Keys.Max());
			var srcs = new Queue<KeyValuePair<DateTime, CommonNode>>(src);
			//var lst = new List<GraphValue>();

			var rslt = ax.Scan(new GraphValue(),(prev, ds) => {
				var gv = new GraphValue() { Date = ds.End };
				var tmp = srcs.Dequeue(a => ds.Start <= a.Key && a.Key <= ds.End);
				if (tmp.IsEmpty()) {
					gv.Amount = prev.Amount;
				}else {
					//期初時価総額
					var st = prev.Amount != 0 ? prev.Amount : tmp.First().Value.Amount;
					//期末時価総額
					gv.Amount = tmp.Last().Value.Amount;
					//キャッシュフローを持つ要素
					var fl = tmp.Where(a => a.Value.InvestmentValue != 0).ToArray();
					//キャッシュフロー合計
					gv.Flow = fl.Sum(a => a.Value.InvestmentValue);

					var vc = gv.Amount - st - gv.Flow;

					var cf = fl.Aggregate(0D,
						(pr, cu) => pr + cu.Value.InvestmentValue * (ds.End - cu.Key).Days / (ds.End - ds.Start).Days);

					var rst = st + cf;
					if (rst != 0)
						gv.Dietz = vc / rst; 
				}
				return gv;
			});
			return rslt.ToArray();
		}
		static IEnumerable<DateSpan> GetTimeAxis(Period period, DateTime start, DateTime end) {
			switch (period) {
			case Period.Weekly:
				var wkax = Ext.weeklyAxis(start, end).ToArray();
				return wkax.Zip(wkax.Select(b => b.AddDays(-7)), (a, b) => new DateSpan(a,b));
			case Period.Monthly:
				var mtax = Ext.monthlyAxis(start, end).ToArray();
				return mtax.Zip(
					mtax.Select(a => new DateTime(a.Year, a.Month, 1)), (a, b) => new DateSpan(a, b));
			case Period.Quarterly:
				var qtax = Ext.quarterlyAxis(start, end).ToArray();
				return qtax.Zip(
					qtax.Select(a => new DateTime(a.Year,a.Month,1).AddMonths(-2)), (a, b) => new DateSpan(a, b));
			case Period.Yearly:
				var yrax = Ext.yearlyAxis(start, end).ToArray();
				return yrax.Zip(yrax.Select(a => new DateTime(a.Year, 1, 1)), (a, b) => new DateSpan(a, b));
			default:
				return Enumerable.Empty<DateSpan>();
			}
		}
		#region date axis static method
		/// <summary>週末日</summary>
		internal static IEnumerable<DateTime> weeklyAxis(DateTime start, DateTime end) {
			DateTime cur = start.DayOfWeek == DayOfWeek.Sunday ? start : start.AddDays(7 - (int)start.DayOfWeek);
			yield return cur;
			while (cur <= end) {
				cur = cur.AddDays(7);
				yield return cur;
			}
		}
		/// <summary>月末日</summary>
		internal static IEnumerable<DateTime> monthlyAxis(DateTime start, DateTime end) {
			var c = EndOfMonth(start);
			yield return c;
			while (c <= end) {
				c = NextEndOfMonth(c, 1);
				yield return c;
			}
		}
		/// <summary>四半期末日</summary>
		internal static IEnumerable<DateTime> quarterlyAxis(DateTime start, DateTime end) {
			int q = start.Month / 3;
			var c = EndOfMonth(new DateTime(start.Year, (q + 1) * 3, 1));
			yield return c;
			while (c <= end) {
				c = NextEndOfMonth(c, 3);
				yield return c;
			}
		}
		/// <summary>年末日</summary>
		internal static IEnumerable<DateTime> yearlyAxis(DateTime start, DateTime end) {
			var c = new DateTime(start.Year, 12, 31);
			yield return c;
			while (c <= end) {
				c = c.AddYears(1);
				yield return c;
			}
		}
		static int DaysInMonth(DateTime dt) {
			return DateTime.DaysInMonth(dt.Year, dt.Month);
		}
		static DateTime EndOfMonth(DateTime dt) {
			return new DateTime(dt.Year, dt.Month, DaysInMonth(dt));
		}
		static DateTime NextEndOfMonth(DateTime dt, int month) {
			var d = new DateTime(dt.Year, dt.Month, 1).AddMonths(month);
			return EndOfMonth(d);
		}
		#endregion
		/// <summary>現在のノードから指定した階層だけ下位のノードを返す。</summary>
		public static IEnumerable<CommonNode> TargetLevels(this CommonNode cur, int tgtLv) {
			var cd = cur.NodeIndex().CurrentDepth;
			var ch = cur.Height();
			var tg = Math.Min(ch, tgtLv) + cd;
			return cur.Levelorder()
				.SkipWhile(a => a.NodeIndex().CurrentDepth < tg)
				.TakeWhile(a => a.NodeIndex().CurrentDepth == tg);
		}
		/// <summary>指定した項目種別で括る。</summary>
		public static IEnumerable<TempValue> MargeNodes(this CommonNode current, int tgtLv,DividePattern div) {
			return current.TargetLevels(tgtLv).MargeNodes(div);
		}
		private class CashNodeCountedTempValue : TempValue {
			public int CashCount { get; set; }
		}
		public static IEnumerable<TempValue> MargeNodes(this IEnumerable<CommonNode> collection, DividePattern div) {
			Func<CommonNode, string> DivFunc;
			switch (div) {
			case DividePattern.Location:
				DivFunc = c => c.Name;
				break;
			default:
				DivFunc = c => c.Tag.TagName;
				break;
			}
			var ttl = (double)(collection.Sum(a => (a.Amount)));
			if (ttl == 0) return Enumerable.Empty<TempValue>();
			return collection
				.ToLookup(DivFunc)
				.Select(a => {
					var tv = new CashNodeCountedTempValue();
					tv.Title = a.Key;
					tv.Amount = a.Sum(b => b.Amount);
					tv.Rate = tv.Amount / ttl * 100;
					tv.CashCount = a.Count(b => b.GetNodeType() == NodeType.Cash);
					return tv;
				})
				.OrderBy(a => a.CashCount);
			
		}
	}
	public class GraphDataManager : DynamicViewModel {

		public GraphDataManager() : base(new GraphMediator()) {
			Model.Initialize(this);
		}
		public void Refresh() => Model.Refresh();
		GraphMediator Model => this.MaybeModelAs<GraphMediator>().Value;

		DateTreeRoot dtr = new DateTreeRoot();
		public ObservableCollection<DateTree> DateList => dtr.Children;

		public BrakeDownList BrakeDown { get; } = new BrakeDownList();
		public TransitionList Transition { get; } = new TransitionList();
		public IndexList Index { get; } = new IndexList();
		public VolatilityList Volatility { get; } = new VolatilityList();

		
		
		private class GraphMediator : NotificationObject {
			GraphDataManager gdm;
			public GraphMediator() {
			}
			public void Initialize(GraphDataManager vm) {
				gdm = vm;
				this._commonNode = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= DateTime.Today)
							?? RootCollection.Instance.FirstOrDefault(a => DateTime.Today <= a.CurrentDate);
				Refresh();
			}
			#region properties
			public DateTime? CurrentDate {
				get { return (CurrentNode?.Root() as TotalRiskFundNode)?.CurrentDate; }
				set {
					if (CurrentDate == value) return;
					// set currentNode
					if (value == null) {
						CurrentNode = null;
					}else {
						var nn = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= value)
							?? RootCollection.Instance.FirstOrDefault(a => value <= a.CurrentDate);
						CurrentNode = nn;
					}
					RaisePropertyChanged();
				}
			}
			CommonNode _commonNode;
			public CommonNode CurrentNode {
				get { return _commonNode; }
				private set {
					if (_commonNode == value) return;
					_commonNode = value;
					RaisePropertyChanged();
					RaisePropertyChanged(() => TargetLevel);
					RefreshBrakeDownList();
					RefreshHistoryList();
				}
			}
			Period _timePeriod;
			public Period TimePeriod {
				get { return _timePeriod; }
				set {
					if (_timePeriod == value) return;
					_timePeriod = value;
					RaisePropertyChanged();
					RefreshHistoryList();
				}
			}
			int _targetLevel = 1;
			public int TargetLevel {
				get {
					if (CurrentNode == null) return _targetLevel;
					var hg = CurrentNode.Height();
					return Math.Min(_targetLevel, hg);
				}
				set {
					if (_targetLevel == value) return;
					_targetLevel = Math.Max(0, value);
					RaisePropertyChanged();
					RefreshBrakeDownList();
					//RefreshHistoryList();
				}
			}
			DividePattern _divide;
			public DividePattern Divide {
				get { return _divide; }
				set {
					if (_divide == value) return;
					_divide = value;
					RaisePropertyChanged();
					RefreshBrakeDownList();
					//RefreshHistoryList();
				}
			}
			TransitionStatus _investmentUnit;
			public TransitionStatus TransitionStatus {
				get { return _investmentUnit; }
				set {
					if (_investmentUnit == value) return;//transition も比較して設定
					_investmentUnit = value;
					RaisePropertyChanged();
					//InvestmentUnitChanged();
					DrawTransitionGraph();
				}
			}
			
			#endregion
			public void Refresh() {
				RefreshBrakeDownList();
				RefreshHistoryList();
			}
			void RefreshBrakeDownList() {
				gdm.BrakeDown.Clear();
				if (CurrentNode == null) return;
				var tgnss = CurrentNode.MargeNodes(TargetLevel, Divide).ToArray();
				foreach (var data in tgnss) {
					gdm.BrakeDown.Add(
						new PieSeries() {
							Title = data.Title,
							Values = new ChartValues<ObservableValue>() { new ObservableValue(data.Rate) },
							DataLabels = true,
						});
				}
			}
			IEnumerable<GraphValue> _GraphRowData = Enumerable.Empty<GraphValue>();
			/// <summary>累計キャッシュフローとその時点での評価総額</summary>
			IEnumerable<Tuple<double,double>> _GraphRowPL {
				get {
					return _GraphRowData
						.Scan(new Tuple<double, double>(0, 0), (ac, el) =>
							   new Tuple<double, double>(ac.Item1 + el.Flow, el.Amount));
				}
			}
			void RefreshHistoryList() {
				if (CurrentDate == null || CurrentNode == null) {
					_GraphRowData = Enumerable.Empty<GraphValue>();
					//return;
				}else {
					_GraphRowData = RootCollection
						.GetNodeLine(this.CurrentNode.Path)
						.ToGraphValues(this.TimePeriod);
				}

				// _______________________________________ Transition initialize
				DrawTransitionGraph();
				// _______________________________________ Volatility initialize
				DrawVolatilityGraph();
				// _______________________________________ Index initialize
				DrawIndexGraph();
			}

			#region Draw Transition Graph
			void DrawTransitionGraph() {
				gdm.Transition.Clear();
				gdm.Transition.Labels = _GraphRowData.Select(a=>a.Date.ToShortDateString());
				if (this.TransitionStatus == TransitionStatus.SingleCashFlow) {
					setBalanceLine();
					setCashFlowColumn();
				}else if(this.TransitionStatus == TransitionStatus.StackCashFlow) {
					setFlowAndProfitLoss();
				}else if(this.TransitionStatus == TransitionStatus.ProfitLossOnly) {
					setProfitLoss();
				}else if(this.TransitionStatus == TransitionStatus.BalanceOnly) {
					setBalanceLine();
				}
			}
			void setBalanceLine() {
				gdm.Transition.Add(new LineSeries() {
					Title = CurrentNode?.Name,
					//Values = new ChartValues<DateTimePoint>(_GraphRowData.Select(a=>new DateTimePoint(a.Date,a.Amount))),
					Values = new ChartValues<double>(_GraphRowData.Select(a => a.Amount)),
				});
			}
			void setCashFlowColumn() {
				gdm.Transition.Add(new ColumnSeries() {
					Title = "Cash Flow",
					Values = new ChartValues<double>(_GraphRowData.Select(a=>a.Flow)),
				});
			}
			void setProfitLoss() {
				gdm.Transition.Add(new LineSeries() {
					Title = "Profit and Loss",
					Values = new ChartValues<double>(_GraphRowPL.Select(a => a.Item2 - a.Item1)),
				});
			}
			void setFlowAndProfitLoss() {
				gdm.Transition.Add(
					new StackedAreaSeries() {
						Title = "Profit and Loss",
						Values = new ChartValues<double>(_GraphRowPL.Select(a=>a.Item2 - a.Item1)),
					});
				gdm.Transition.Add(
					new StackedAreaSeries() {
						Title = "Cash Flow",
						Values = new ChartValues<double>(_GraphRowPL.Select(a=>a.Item1)),
					});
			}
			#endregion

			#region Draw Index Graph
			void DrawIndexGraph() {
				gdm.Index.Clear();
				gdm.Index.Labels = _GraphRowData.Select(a => a.Date.ToShortDateString());
				gdm.Index.Add(new LineSeries() {
					Title = "Index",
					Values = new ChartValues<double>(
						_GraphRowData
							.Scan(1d, (pr, cu) => pr * cu.Dietz)
							.Select(a => a * 100)),
				});
			}
			#endregion

			#region Draw Volatility Graph
			void DrawVolatilityGraph() {
				gdm.Volatility.Clear();
				gdm.Volatility.Labels = _GraphRowData.Select(a=>a.Date.ToShortDateString());
				gdm.Volatility.Add(new LineSeries() {
					Title = "Volatility",
					Values = new ChartValues<double>(_GraphRowData.Select(a => (a.Dietz - 1) * 100)),
				});
			}
			#endregion
		}
	}
	public class BrakeDownList : SeriesCollection { }
	public class TransitionList : DisplaySeriesCollection { }
	public class IndexList : DisplaySeriesCollection { }
	public class VolatilityList : DisplaySeriesCollection { }

	public class DisplaySeriesCollection : SeriesCollection {
		
		IEnumerable<string> _labels = Enumerable.Empty<string>();
		public IEnumerable<string> Labels {
			get { return _labels; }
			set {
				if (_labels.SequenceEqual(value)) return;
				_labels = value;
				base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Labels)));
			}
		}
	}
}
