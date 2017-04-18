using Houzkin;
using Houzkin.Architecture;
using Houzkin.Tree;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Livet;
using Livet.Commands;
using Livet.EventListeners.WeakEvents;
using Livet.Messaging;
using PortFolion.Core;
using PortFolion.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PortFolion.ViewModels {
	public enum Period {
		Weekly,
		Monthly,
		Quarterly,
		Yearly,
		//EachWrittenDate,
	}
	public enum DividePattern {
		Location,
		Tag,
	}
	public enum TransitionStatus {
		/// <summary>推移</summary>
		BalanceOnly,
		/// <summary>推移のラインと外部キャッシュフローをカラムにして表す</summary>
		SingleCashFlow,
		/// <summary>推移のラインと外部キャッシュフローの累積を表す</summary>
		StackCashFlow,
		/// <summary>損益のみ</summary>
		ProfitLossOnly,
	}
	
	public class TempValue : SeriesViewModel {
		//public string Title { get; set; }
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
			var ax = Ext.GetTimeAxis(period, src.Keys);
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
		static IEnumerable<DateSpan> GetTimeAxis(Period period, IEnumerable<DateTime> dates) {
			DateTime start = dates.Min();
			DateTime end = dates.Max();
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
				var erd = dates.Take(1).Concat(dates.Select(a => a.AddDays(1)));
				return dates.Zip(erd, (a, b) => new DateSpan(a, b));
				//return Enumerable.Empty<DateSpan>();
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
					tv.Rate = ((double)tv.Amount / (double)ttl);
					tv.CashCount = a.Count(b => b.GetNodeType() == NodeType.Cash);
					return tv;
				})
				.GroupBy(a => a.CashCount)
				.OrderBy(a => a.Key)
				.SelectMany(a => a.OrderByDescending(b => b.Amount));
				//.OrderBy(a => a.CashCount);
			
		}
		public static IEnumerable<Color> BrushColors() {
			//object obj = ColorConverter.ConvertFromString("#51000000");
			//SolidColorBrush ret = new SolidColorBrush((System.Windows.Media.Color)obj);
			return new List<Color>() {
				Color.FromRgb(17,140,18),
				Color.FromRgb(214,54,96),
				Color.FromRgb(59,67,255),
				Color.FromRgb(167,68,227),
				Color.FromRgb(237,103,30),

				Color.FromRgb(76,143,44),
				Color.FromRgb(44,50,143),
				Color.FromRgb(248,63,27),
				Color.FromRgb(130,16,142),
				Color.FromRgb(21,157,184),

				Color.FromRgb(201,15,41),
				Color.FromRgb(216,105,26),
				Color.FromRgb(7,109,48),
				Color.FromRgb(73,13,147),
				Color.FromRgb(221,39,124),

				Color.FromRgb(225,116,223),
				Color.FromRgb(98,183,189),
				Color.FromRgb(132,79,175),
				Color.FromRgb(255,239,0),
				Color.FromRgb(175,176,0),

				Color.FromRgb(199,27,76),
				Color.FromRgb(170,119,10),
				Color.FromRgb(49,226,239),
				Color.FromRgb(33,145,242),
				Color.FromRgb(21,234,152),
			};
		}
	}
	public class GraphDataManager : DynamicViewModel {

		public GraphDataManager() : base(new GraphMediator()) {
			Model.Initialize(this);
			
		}
		private InteractionMessenger _messenger;
		public InteractionMessenger Messenger {
			get { return _messenger = _messenger ?? new InteractionMessenger(); }
			set { _messenger = value; }
		}
		public void Refresh() => Model.Refresh();
		GraphMediator Model => this.MaybeModelAs<GraphMediator>().Value;

		DateTreeRoot dtr = new DateTreeRoot();
		public ObservableCollection<DateTree> DateList => dtr.Children;

		public int TargetLevel {
			get { return this.Model.TargetLevel; }
			set { this.Model.TargetLevel = value; }
		}
		#region edit params
		[Flags]
		enum displayParams {
			none = 0,
			period = 1,
			level = 2,
			trans = 4,
			all = period | level | trans,
		}
		public void EnableParams() {
			if (VisibleParams) {
				VisibleParams = false;
				setDisplayParams(displayParams.none);
			}else {
				VisibleParams = true;
				setDisplayParams(displayParams.all);
			}
		}
		public void EnableBrakeDownParams() {
			var ep = displayParams.level;
			if (VisibleParams && ep == getDisplayParams()) {
				VisibleParams = false;
				setDisplayParams(displayParams.none);
			}else {
				VisibleParams = true;
				setDisplayParams(ep);
			}
		}
		public void EnableTranstionParams() {
			var ep = displayParams.period | displayParams.trans;
			if(VisibleParams && ep == getDisplayParams()) {
				VisibleParams = false;
				setDisplayParams(displayParams.none);
			}else {
				VisibleParams = true;
				setDisplayParams(ep);
			}
		}
		
		bool _visibleParams;
		public bool VisibleParams {
			get { return _visibleParams; }
			set {
				SetProperty(ref _visibleParams, value);
				if(value)Messenger.Raise(new InteractionMessage("ExpandSandP"));
			}
		}
		void setDisplayParams(displayParams ep) {
			if (ep.HasFlag(displayParams.period)) EnablePeriodTime = true;
			else EnablePeriodTime = false;

			if (ep.HasFlag(displayParams.level)) EnableTagetLevel = true;
			else EnableTagetLevel = false;

			if (ep.HasFlag(displayParams.trans)) EnableTransitionStatus = true;
			else EnableTransitionStatus = false;
		}
		displayParams getDisplayParams() {
			displayParams ep = displayParams.none;
			if (EnablePeriodTime) ep |= displayParams.period;
			if (EnableTagetLevel) ep |= displayParams.level;
			if (EnableTransitionStatus) ep |= displayParams.trans;
			return ep;
		}

		bool _enablePeriodTime;
		public bool EnablePeriodTime {
			get { return _enablePeriodTime; }
			set { SetProperty(ref _enablePeriodTime, value); }
		}
		bool _enableTargetLevel;
		public bool EnableTagetLevel {
			get { return _enableTargetLevel; }
			set { SetProperty(ref _enableTargetLevel, value); }
		}
		bool _enableTransition;
		public bool EnableTransitionStatus {
			get { return _enableTransition; }
			set { SetProperty(ref _enableTransition, value); }
		}
		#endregion
		BrakeDownList bdl;
		public BrakeDownList BrakeDown {
			get { return bdl; }
			set {
				SetProperty(ref bdl, value) ;
					//OnPropertyChanged(() => BrakeDownLegend);
			}
		}
		IEnumerable<TempValue> _bdLegend;
		public IEnumerable<TempValue> BrakeDownLegend {
			get { return _bdLegend; }
			set { SetProperty(ref _bdLegend, value); }
		}
		TransitionList tsl;
		public TransitionList Transition {
			get { return tsl; }
			set { SetProperty(ref tsl, value); }
		}
		IndexList idl;
		public IndexList Index {
			get { return idl; }
			set { SetProperty(ref idl, value); }
		}
		VolatilityList vll;
		public VolatilityList Volatility {
			get { return vll; }
			set { SetProperty(ref vll, value); }
		}
		
		private class GraphMediator : ViewModel {
			GraphDataManager gdm;
			public GraphMediator() {
				var cmp = Mappers.Pie<TempValue>().Value(tv => tv.Amount);
				Charting.For<TempValue>(cmp);
			}
			public void Initialize(GraphDataManager vm) {
				gdm = vm;
				this.CurrentNode = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= DateTime.Today)
							?? RootCollection.Instance.FirstOrDefault(a => DateTime.Today <= a.CurrentDate);

				this.CompositeDisposable.Add(
					new CollectionChangedWeakEventListener(RootCollection.Instance, (o, e) => {
						gdm.dtr.Refresh();
						this.CurrentDate = CurrentDate;
					}));
				var d = new LivetWeakEventListener<EventHandler<DateTimeSelectedEventArgs>, DateTimeSelectedEventArgs>(
					h => h,
					h => gdm.dtr.DateTimeSelected += h,
					h => gdm.dtr.DateTimeSelected -= h,
					(s, e) => this.CurrentDate = e.SelectedDateTime);
				this.CompositeDisposable.Add(d);
				
			}

			#region properties

			public DateTime? CurrentDate {
				get { return (CurrentNode?.Root() as TotalRiskFundNode)?.CurrentDate; }
				set {
					if (RootCollection.Instance.Contains(CurrentNode?.Root()) && CurrentDate == value) return;
					// set currentNode
					if (value == null) {
						CurrentNode = null;
					}else {
						var nn = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= value)
							?? RootCollection.Instance.FirstOrDefault(a => value <= a.CurrentDate);
						if(CurrentNode != null) {
							CurrentNode = nn.SearchNodeOf(CurrentNode.Path);
						}else {
							CurrentNode = nn;
						}
					}
					RaisePropertyChanged();
				}
			}
			public ObservableCollection<LocationRoot> Root { get; } = new ObservableCollection<LocationRoot>();
			void setRoot(TotalRiskFundNode rt) {
				if (Root.Any(a => a.IsModelEquals(rt))) return;
				Root.ForEach(a => {
					// remove events
					a.Selected -= locationSelected;
				});
				List<NodePath<string>> expns = new List<NodePath<string>>();
				if (Root.Any()) {
					var ls = Root.First().Preorder().Where(a => a.IsExpand).Select(a => a.Path);
					expns.AddRange(ls);
				}
				Root.Clear();
				if (rt != null) {
					var ln = new LocationRoot(rt);
					// add events
					ln.Selected += locationSelected;
					Root.Add(ln);
					ln.Preorder().Where(a => expns.Any(b => b.SequenceEqual(a.Path)))
						.ForEach(a => a.IsExpand = true);
				}
			}
			void locationSelected(object o,LocationSelectedEventArgs e) {
				this.CurrentNode = e.Location;
			}
			CommonNode _commonNode;
			public CommonNode CurrentNode {
				get { return _commonNode; }
				private set {
					if (_commonNode == value) return;
					if (_commonNode?.Root() != value?.Root())
						setRoot(value?.Root() as TotalRiskFundNode);
					var prvPath = _commonNode?.Path ?? Enumerable.Empty<string>();
					var curPath = value?.Path ?? Enumerable.Empty<string>();
					_commonNode = value;
					RaisePropertyChanged();
					RaisePropertyChanged(() => TargetLevel);
					RefreshBrakeDownList();
					if (!prvPath.SequenceEqual(curPath)) {
						RefreshHistoryList();
					}
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
					var hg = CurrentNode?.Height() ?? 0;
					value = Math.Max(0, Math.Min(hg, value));
					if (TargetLevel == value) return;
					_targetLevel = value;
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
				gdm.BrakeDown = new BrakeDownList();
				if (CurrentNode == null) return;
				var tgnss = CurrentNode.MargeNodes(TargetLevel, Divide).ToArray();
				tgnss.Zip(Ext.BrushColors().Repeat(), (a, b) => new { Data = a, Brush = b })
					.ForEach(a => {
						a.Data.Fill = new SolidColorBrush(a.Brush);
						a.Data.Stroke = new SolidColorBrush(Color.Multiply(a.Brush, 0.5f));
						a.Data.StrokeThickness = 5;
						var ps = new PieSeries() {
								Title = a.Data.Title,
								//Values = new ChartValues<ObservableValue>() { new ObservableValue(a.Data.Amount) },
								Values = new ChartValues<TempValue>() { a.Data },
								DataLabels = true,
								//LabelPoint = cp => string.Format("{0}\n({1:P})", a.Data.Title, cp.Participation),
								LabelPoint = cp => a.Data.Title,
								LabelPosition = PieLabelPosition.OutsideSlice,
								//StrokeThickness = 5,
								//Fill = new SolidColorBrush(a.Brush),
								//Stroke = new SolidColorBrush(Color.Multiply(a.Brush, 0.5f)),
								StrokeThickness = a.Data.StrokeThickness,
								Fill = a.Data.Fill,
								Stroke = a.Data.Stroke,
							};
						a.Data.PointGeometry = ps.PointGeometry;
						gdm.BrakeDown.Add(ps);
							
					});
				gdm.BrakeDownLegend = tgnss;
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
				//gdm.Transition.Clear();new DateTime((long)x).ToString("yyyy/M/d")
				gdm.Transition = gdm.Transition ?? new TransitionList() {
					//XFormatter = x => new DateTime((long)x).ToString("yyyy/M/d"),
					YFormatter = y => y.ToString("#,0.#"),
				};
				var cnt = gdm.Transition.Count;
				while(0 < cnt) {
					cnt--;
					gdm.Transition.RemoveAt(cnt);
				}
				gdm.Transition.Labels = _GraphRowData.Select(a => a.Date.ToString("yyyy/M/d")).ToArray();
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
					//Values = new ChartValues<DateTimePoint>(_GraphRowData.Select(a => new DateTimePoint(a.Date, a.Amount))),
					LineSmoothness = 0,
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
					LineSmoothness = 0,
				});
			}
			void setFlowAndProfitLoss() {
				gdm.Transition.Add(
					new StackedAreaSeries() {
						Title = "Cash Flow",
						Values = new ChartValues<double>(_GraphRowPL.Select(a=>a.Item1)),
					});
				gdm.Transition.Add(
					new StackedAreaSeries() {
						Title = "Profit and Loss",
						Values = new ChartValues<double>(_GraphRowPL.Select(a=>a.Item2 - a.Item1)),
					});
			}
			#endregion

			#region Draw Index Graph
			void DrawIndexGraph() {
				//gdm.Index.Clear();
				gdm.Index = new IndexList();
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
				//gdm.Volatility.Clear();
				gdm.Volatility = new VolatilityList();
				gdm.Volatility.Labels = _GraphRowData.Select(a=>a.Date.ToShortDateString());
				gdm.Volatility.Add(new LineSeries() {
					Title = "Volatility",
					Values = new ChartValues<double>(_GraphRowData.Select(a => (a.Dietz - 1) * 100)),
				});
			}
			#endregion
		}
	}
	public class BrakeDownList : SeriesCollection {
	}
	public class TransitionList : DisplaySeriesCollection { }
	public class IndexList : DisplaySeriesCollection { }
	public class VolatilityList : DisplaySeriesCollection { }

	public class DisplaySeriesCollection : SeriesCollection {
		
		public DisplaySeriesCollection() {
			RangeChangedCmd = new ViewModelCommand(rangeChanged);
		}
		IEnumerable<string> _labels = Enumerable.Empty<string>();
		public IEnumerable<string> Labels {
			get { return _labels; }
			set {
				if (_labels.SequenceEqual(value)) return;
				_labels = value;
				base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Labels)));
			}
		}
		double min = double.NaN;
		public double DisplayMinValue {
			get { return min; }
			set {
				if (min == value) return;
				min = value;
				base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(DisplayMinValue)));
				//base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(DisplayMinValue)));
			}
		}
		double max = double.NaN;
		public double DisplayMaxValue {
			get { return max; }
			set {
				if (max == value) return;
				max = value;
				base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(DisplayMaxValue)));
			}
		}
		public double MaxLimit => Math.Max(0d, Labels?.Count() -1 ?? 0d);
		//public Func<double,string> XFormatter { get; set; }
		public Func<double, string> YFormatter { get; set; }

		public ViewModelCommand RangeChangedCmd { get; set; }
		void rangeChanged() {
			double rng = max - min;
			if(min < 0) {
				DisplayMinValue = 0;
				DisplayMaxValue = Math.Min(rng, MaxLimit);
			}
			if (MaxLimit < max) {
				DisplayMaxValue = MaxLimit;
				DisplayMinValue = Math.Max(0, MaxLimit - rng);
			}
		}
	}
}
