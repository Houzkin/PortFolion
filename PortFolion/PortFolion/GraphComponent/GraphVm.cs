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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PortFolion.ViewModels {
	public class GraphTabViewModel : ViewModel {

		public GraphTabViewModel() {
			this.CompositeDisposable.Add(
				new CollectionChangedWeakEventListener(RootCollection.Instance, (o, e) => {
					dtr.Refresh();
					this.CurrentDate = CurrentDate;
				}));
			var d = new LivetWeakEventListener<EventHandler<DateTimeSelectedEventArgs>, DateTimeSelectedEventArgs>(
				h => h,
				h => dtr.DateTimeSelected += h,
				h => dtr.DateTimeSelected -= h,
				(s, e) => this.CurrentDate = e.SelectedDateTime);
			this.CompositeDisposable.Add(d);


			this.PropertyChanged += (o, e) => {
				if (e.PropertyName == nameof(this.CurrentPath) || e.PropertyName == nameof(this.CurrentNode)
				|| e.PropertyName == nameof(this.Divide) || e.PropertyName == nameof(this.TimePeriod)
				|| e.PropertyName == nameof(this.TargetLevel)) {
					var t = _mng.Update();
					this.BrakeDownInner.Update();
					this.BrakeDown.Update();
					t.Wait();
					foreach (var g in this.Graphs) {
						g.Update();
					}
				}
			};

			this.Graphs.CollectionChanged += (o, e) => {
				foreach(var g in this.Graphs) {
					g.IndexUpCommand.RaiseCanExecuteChanged();
					g.IndexDownCommand.RaiseCanExecuteChanged();
				}
			};


			_mng = new gvMng(this);
			BrakeDown = new BrakeDownChart(this);
			BrakeDownInner = new BrakeDownInnerChart(this);
			this.CurrentNode = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= DateTime.Today)
							?? RootCollection.Instance.FirstOrDefault(a => DateTime.Today <= a.CurrentDate);
			if (this.CurrentNode != null)
				this.dtr.SelectAt((CurrentNode.Root() as TotalRiskFundNode).CurrentDate);
		}
		gvMng _mng;

		public void Refresh() {
			if (this.CurrentNode == null) {
				this.CurrentNode = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= DateTime.Today)
								?? RootCollection.Instance.FirstOrDefault(a => DateTime.Today <= a.CurrentDate);
				return;
			}
			var t = _mng.Refresh();
			BrakeDownInner.Refresh();
			BrakeDown.Refresh();
			Ext.ResetColorIndex();
			t.Wait();
			foreach (var g in Graphs) g.Refresh();
		}
		

		DateTreeRoot dtr = new DateTreeRoot();
		public ObservableCollection<DateTree> DateList => dtr.Children;

		public ObservableCollection<LocationRoot> Root { get; } = new ObservableCollection<LocationRoot>();
		void setRoot(TotalRiskFundNode rt, CommonNode cur) {
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
				var ln = new LocationRoot(rt,cur);
				// add events
				ln.Selected += locationSelected;
				Root.Add(ln);
				ln.Preorder().Where(a => expns.Any(b => b.SequenceEqual(a.Path)))
					.ForEach(a => a.IsExpand = true);
			}
		}
		void locationSelected(object o, LocationSelectedEventArgs e) {
			this.CurrentNode = e.Location;
		}
		#region props
		public DateTime? CurrentDate {
			get { return (CurrentNode?.Root() as TotalRiskFundNode)?.CurrentDate; }
			private set {
				if (RootCollection.Instance.Contains(CurrentNode?.Root()) && CurrentDate == value) return;
				// set currentNode
				if (value == null) {
					CurrentNode = null;
				} else {
					var nn = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= value)
						?? RootCollection.Instance.FirstOrDefault(a => value <= a.CurrentDate);
					if (CurrentNode != null && nn != null) {
						CurrentNode = nn.SearchNodeOf(CurrentNode.Path);
					} else {
						CurrentNode = nn;
					}
				}
				RaisePropertyChanged();
			}
		}

		CommonNode _commonNode;
		public CommonNode CurrentNode {
			get { return _commonNode; }
			private set {
				if (_commonNode == value) return;
				if (_commonNode?.Root() != value?.Root())
					setRoot(value?.Root() as TotalRiskFundNode, value);
				_commonNode = value;
				CurrentPath = value?.Path ?? Enumerable.Empty<string>();
				RaisePropertyChanged(() => TargetLevel);
				RaisePropertyChanged();
			}
		}
		IEnumerable<string> _currentPath = Enumerable.Empty<string>();
		public IEnumerable<string> CurrentPath {
			get { return _currentPath; }
			private set {
				if (_currentPath.SequenceEqual(value)) return;
				_currentPath = value;
				//_mng.Update();
				RaisePropertyChanged();
			}
		}
		int _targetLv = 1;
		public int TargetLevel {
			get {
				if (CurrentNode == null) return _targetLv;
				var hg = CurrentNode.Height();
				return Math.Min(_targetLv, hg);
			}
			set {
				var hg = CurrentNode?.Height() ?? 0;
				value = Math.Max(0, Math.Min(hg, value));
				if (TargetLevel == value) return;
				_targetLv = value;
				RaisePropertyChanged();
			}
		}
		Period _timePeriod;
		public Period TimePeriod {
			get { return _timePeriod; }
			set {
				if (_timePeriod == value) return;
				_timePeriod = value;
				RaisePropertyChanged();
			}
		}
		DividePattern _divide;
		public DividePattern Divide {
			get { return _divide; }
			set {
				if (_divide == value) return;
				_divide = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		public BrakeDownChart BrakeDown { get; }
		public BrakeDownInnerChart BrakeDownInner{ get; }

		public ObservableCollection<GraphVmBase> Graphs { get; } = new ObservableCollection<GraphVmBase>();

		#region graphs
		BalanceSeries bls;
		public bool VisibleBalance {
			get { return !(bls == null || bls.IsDisposed); }
			set {
				if (!value) {
					bls?.Dispose();
				} else if(value && !VisibleBalance) {
					bls = new BalanceSeries(this, a => _mng.GraphData);
					Graphs.Insert(0, bls);
					bls.Disposed += (o, e) => {
						Graphs.Remove(bls);
						RaisePropertyChanged();
					};
					bls.Refresh();
				}
			}
		}

		IndexGraphVm igvm;
		public bool VisibleIndex {
			get { return !(igvm == null || igvm.IsDisposed); }
			set {
				if (!value) {
					igvm?.Dispose();
				}else if(value && !VisibleIndex) {
					igvm = new IndexGraphVm(this, a => _mng.GraphData);
					Graphs.Insert(0, igvm);
					igvm.Disposed += (o, e) => {
						Graphs.Remove(igvm);
						RaisePropertyChanged();
					};
					igvm.Refresh();
				}
			}
		}

		VolatilityGraphVm vgvm;
		public bool VisibleVolatility {
			get { return !(vgvm == null || vgvm.IsDisposed); }
			set {
				if (!value) {
					vgvm?.Dispose();
				}else if(value && !VisibleVolatility) {
					vgvm = new VolatilityGraphVm(this, a => _mng.GraphData);
					Graphs.Insert(0, vgvm);
					vgvm.Disposed += (o, e) => {
						Graphs.Remove(vgvm);
						RaisePropertyChanged();
					};
					vgvm.Refresh();
				}
			}
		}
		#endregion

        /// <summary>推移グラフの描画に対して操作するためのクラス</summary>
		class gvMng {
			GraphTabViewModel gtvm;
			IEnumerable<string> _curPath = Enumerable.Empty<string>();
			Period _period;

			public gvMng(GraphTabViewModel vm) {
				gtvm = vm;
				//Refresh();
			}
			public Task Update() {
				var t = Task.Run(() => {
					if (!_curPath.SequenceEqual(gtvm.CurrentPath) || _period != gtvm.TimePeriod) {
						Refresh().Wait();
					}
				});
				return t;
			}
			public Task Refresh() {
				_curPath = gtvm.CurrentPath;
				_period = gtvm.TimePeriod;
				var t = Task.Run(() => {
					GraphData = RootCollection
						.GetNodeLine(_curPath)
						.ToGraphValues(_period);
				});
				return t;
			}
			IEnumerable<PlotValue> _graphData;
			public IEnumerable<PlotValue> GraphData {
				get { return _graphData = _graphData ?? Enumerable.Empty<PlotValue>(); }
				private set { _graphData = value; }
			}
		}
        /// <summary>内訳チャート部分を操作するクラス</summary>
		class bdMng {
			IEnumerable<string> _curPath = Enumerable.Empty<string>();
			Period _period;
			DividePattern _divide;
			int _lvl;
			CommonNode _curNode;
			GraphTabViewModel gtvm;

			public bdMng(GraphTabViewModel vm) {
				gtvm = vm;
			}
			public Task Refresh() {
				_curPath = gtvm.CurrentPath;
				_period = gtvm.TimePeriod;
				_curNode = gtvm.CurrentNode;
				_divide = gtvm.Divide;
				_lvl = gtvm.TargetLevel;
				var t = Task.Run(() => {
					if (_curNode == null) return;
					//var m = _curNode.MargeNodes(_lvl, _divide);
					//GraphData = RootCollection.GetNodeLine(_curPath);
				});
				return t;
			}
			public object GraphData { get; set; }
		}
	}
    /// <summary>内訳チャートのVM</summary>
	public class BrakeDownChart : SeriesCollection {
		
		GraphTabViewModel _vm;

		int _targetLv;
		DividePattern _divide;
		CommonNode _curNode;

		public BrakeDownChart(GraphTabViewModel viewModel):base(Mappers.Pie<SeriesValue>().Value(tv=>tv.Amount)) {
			_vm = viewModel;
			Refresh();
		}
		/// <summary>変更があった場合、更新する</summary>
		public void Update() {
			if(_targetLv != _vm.TargetLevel || _divide != _vm.Divide || _curNode != _vm.CurrentNode) {
				
				Refresh();
			}
		}
		/// <summary>現在の条件で再描画する</summary>
		public void Refresh() {
			_targetLv = _vm.TargetLevel;
			_divide = _vm.Divide;
			_curNode = _vm.CurrentNode;

			var cnt = this.Count;
			while (0 < cnt) {
				cnt--;
				this.RemoveAt(cnt);
			}
			if (_curNode == null) return;
			SetSeriesAndLegend(_targetLv,_divide,_curNode);
		}
		protected virtual void SetSeriesAndLegend(int tgtLv, DividePattern div, CommonNode con,bool colorRv = false){
			var tgnss = con.MargeNodes(tgtLv, div).ToArray();
			var colors = colorRv ? Ext.PieBrushColors(tgnss.Length).AsEnumerable().Reverse() : Ext.PieBrushColors(tgnss.Length);
			tgnss.Zip(colors, (a, b) => new { Data = a, Brush = b })
				.ForEach((a,i) => {
					a.Data.Fill = new SolidColorBrush(a.Brush);
					a.Data.Stroke = new SolidColorBrush(Color.Multiply(a.Brush, 0.5f));
					a.Data.StrokeThickness = 3;
					var ps = new PieSeries() {
						Title = a.Data.Title,
						Values = new ChartValues<SeriesValue>() { a.Data },
						DataLabels = true,
						LabelPoint = cp => a.Data.Title,
						LabelPosition = PieLabelPosition.OutsideSlice,
						StrokeThickness = a.Data.StrokeThickness,
						Fill = a.Data.Fill,
						Stroke = a.Data.Stroke,
					};
					//if (i % 2 == 0) ps.LabelPosition = PieLabelPosition.InsideSlice;
					a.Data.PointGeometry = ps.PointGeometry;
					this.Add(ps);
				});
			this.BrakeDownLegend = tgnss;
		}
		IEnumerable<SeriesValue> _legend;
		public IEnumerable<SeriesValue> BrakeDownLegend {
			get { return _legend; }
			protected set {
				if (_legend == value) return;
				_legend = value;
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(BrakeDownLegend)));
			}
		}
	}
	/// <summary>
	/// 内訳チャートの内部を担うVM
	/// tagのみの表示を想定
	/// </summary>
	public class BrakeDownInnerChart:BrakeDownChart{
		public BrakeDownInnerChart(GraphTabViewModel viewModel) : base(viewModel){ }
		protected override void SetSeriesAndLegend(int tgtLv, DividePattern div, CommonNode con,bool colorRv = false) {
			if (div != DividePattern.LocationAndTag)
				return;
			base.SetSeriesAndLegend(tgtLv, DividePattern.Tag, con, true);
			this.OfType<PieSeries>().ForEach(a => a.LabelPosition = PieLabelPosition.InsideSlice);
		}
	}

	public abstract class GraphVmBase : NotificationObject, IDisposable {
		protected GraphTabViewModel ViewModel { get; }

		public GraphVmBase(GraphTabViewModel viewModel,Func<GraphTabViewModel,IEnumerable<object>> getSrc) {
			this.SeriesList = new SeriesCollection();
			ViewModel = viewModel;
			_getSrc = getSrc;
		}
		Func<GraphTabViewModel, IEnumerable<object>> _getSrc { get; }
		protected IEnumerable<object> GetSource() {
			return _getSrc(ViewModel);
		}
		/// <summary>変更があった場合更新する</summary>
		public abstract void Update();
		/// <summary>現在の条件で再描画する</summary>
		public virtual void Refresh() {
			this.RaisePropertyChanged(nameof(MaxLimit));
			this.DisplayMinValue = 0;
			this.DisplayMaxValue = this.MaxLimit;
		}
		
		public SeriesCollection SeriesList { get; private set; }
		protected void RemoveAll() {
			var cnt = this.SeriesList.Count;
			while (0 < cnt) {
				cnt--;
				try {
					this.SeriesList.RemoveAt(cnt);
				} catch { /*ignore*/ }
			}
		}
		IEnumerable<string> _labels = Enumerable.Empty<string>();
		public IEnumerable<string> Labels {
			get { return _labels; }
			set {
				if (_labels.SequenceEqual(value)) return;
				_labels = value;
				this.RaisePropertyChanged();
			}
		}
		IEnumerable<SeriesViewModel> _legend = Enumerable.Empty<SeriesViewModel>();
		public IEnumerable<SeriesViewModel> Legends {
			get { return _legend; }
			set {
				if (_legend.SequenceEqual(value)) return;
				_legend = value;
				this.RaisePropertyChanged();
			}
		}
		double min;
        /// <summary>表示最下端</summary>
		public double DisplayMinValue {
			get { return min; }
			set {
				if (min == value) return;
				min = value;
				this.RaisePropertyChanged();
			}
		}
		double max = 1;
		public double DisplayMaxValue {
			get { return Math.Max(1,max); }
			set {
				if (max == value) return;
				//if (value < 1) max = 1;
				else max = value;
				this.RaisePropertyChanged();
			}
		}
		public virtual double MaxLimit => Math.Max(0d, Labels?.Count() -1 ?? 0d);
		//public Func<double,string> XFormatter { get; set; }
		public Func<double, string> YFormatter => IsLogChart ? LogYFormatter : NormalYFormatter;
		protected virtual Func<double,string> NormalYFormatter => y => y.ToString("#,0.#");
		Func<double, string> LogYFormatter => y => NormalYFormatter(Math.Pow(10, y));

		public bool IsDisposed { get; private set; }
		public void Dispose() {
			if (IsDisposed) return;
			this.RemoveAll();
			IsDisposed = true;
			Disposed?.Invoke(this, new EventArgs());
		}
		public event EventHandler Disposed;

		ViewModelCommand _upcmd;
        /// <summary>グラフの位置を下へ移動</summary>
		public ViewModelCommand IndexUpCommand {
			get {
				if(_upcmd == null) {
					_upcmd = new ViewModelCommand(() => {
						var i = this.ViewModel.Graphs.IndexOf(this);
						this.ViewModel.Graphs.Move(i, i - 1);
					}, () => 
						0 < this.ViewModel.Graphs.IndexOf(this)
					);
				}
				return _upcmd;
			}
		}
		ViewModelCommand _downcmd;
        /// <summary>グラフの位置を上へ移動</summary>
		public ViewModelCommand IndexDownCommand {
			get {
				if(_downcmd == null) {
					_downcmd = new ViewModelCommand(() => {
						var i = this.ViewModel.Graphs.IndexOf(this);
						this.ViewModel.Graphs.Move(i, i + 1);
					}, () =>
						this.ViewModel.Graphs.IndexOf(this) < this.ViewModel.Graphs.Count - 1
					);
				}
				return _downcmd;
			}
		}
		#region menu
        /// <summary>表示方法に対してメニューを持つかどうかを示す値。</summary>
		public virtual bool VisibilityMenu => false;
		bool _isMenuOpen;
        /// <summary>メニューの開閉</summary>
		public bool IsMenuOpen {
			get { return _isMenuOpen; }
			set {
				if (_isMenuOpen == value) return;
				_isMenuOpen = value;
				this.RaisePropertyChanged();
			}
		}
		bool _isLogChart;
        /// <summary>対数表示かどうかを示す値を返す。</summary>
		public bool IsLogChart {
			get { return _isLogChart; }
			set {
				if (_isLogChart == value) return;
				_isLogChart = value;
				if (value) {
					this.SeriesList.Configuration = Mappers.Xy<double>().Y(a => a <= 0 ? 0 : Math.Log10(a));
				}else {
					this.SeriesList.Configuration = null;
				}
				RaisePropertyChanged();
				RaisePropertyChanged(nameof(YFormatter));
			}
		}
		#endregion
	}
    /// <summary>
    /// 推移＋内訳(未完成
    /// </summary>
	public class RateTransition : GraphVmBase {
        /*
         * 指数ベース、残高ベース、百分率のパターン(案
         * */
		CommonNode _curNode;
		Period _period;
		DividePattern _divide;
		int _level;

		public RateTransition(GraphTabViewModel viewModel, Func<GraphTabViewModel, IEnumerable<object>> getSrc)
			: base(viewModel, getSrc) { }

		public override void Update() {
			this.update(GetSource().OfType<KeyValuePair<SeriesValue,Dictionary<DateTime,double>>>());
		}
		public override void Refresh() {
			this.refresh(GetSource().OfType<KeyValuePair<SeriesValue,Dictionary<DateTime,double>>>());
			base.Refresh();
		}
		void update(IEnumerable<KeyValuePair<SeriesValue,Dictionary<DateTime,double>>> src) {
			if(_curNode != ViewModel.CurrentNode
				|| _level != ViewModel.TargetLevel
				|| _divide != ViewModel.Divide
				|| _period != ViewModel.TimePeriod) {
				refresh(src);
				base.Refresh();
			}
		}
		void refresh(IEnumerable<KeyValuePair<SeriesValue,Dictionary<DateTime,double>>> src) {
			_curNode = ViewModel.CurrentNode;
			_level = ViewModel.TargetLevel;
			_divide = ViewModel.Divide;
			_period = ViewModel.TimePeriod;

			RemoveAll();
			//Labels = null;
			foreach(var s in src) {
				var st = new StackedAreaSeries();

			}
			//Legends = null;
		}
	}
    /// <summary>推移を示すグラフのベースクラス</summary>
	public abstract class PathPeriodGraph : GraphVmBase {
		IEnumerable<string> _curPath;
		Period _period;
		public PathPeriodGraph(GraphTabViewModel viewModel,Func<GraphTabViewModel,IEnumerable<object>> getSrc)
			: base(viewModel,getSrc) { }

		public override void Update() {
			this.update(GetSource().OfType<PlotValue>());
		}
		public override void Refresh() {
			this.refresh(GetSource().OfType<PlotValue>());
			base.Refresh();
		}

		void update(IEnumerable<PlotValue> src) {
			if (!_curPath.SequenceEqual(ViewModel.CurrentPath) || _period != ViewModel.TimePeriod) {
				refresh(src);
				base.Refresh();
			}
		}
		void refresh(IEnumerable<PlotValue> src) {
			_curPath = ViewModel.CurrentPath;
			_period = ViewModel.TimePeriod;

			RemoveAll();

			Labels = this.GetLabels(src).ToArray();

			Draw(src);
			Legends = this.SeriesList.OfType<Series>().Select(a => this.ToLegends(a)).ToArray();
		}
		
		protected abstract void Draw(IEnumerable<PlotValue> src);

		protected virtual IEnumerable<string> GetLabels(IEnumerable<PlotValue> src) {
			return src.Select(a => a.Date.ToString("yyyy/M/d"));
		}
		protected virtual SeriesViewModel ToLegends(Series seri) {
			var vm = new SeriesViewModel {
				Title = seri.Title,
				Fill = seri.Fill,
				Stroke = seri.Stroke,
				StrokeThickness = seri.StrokeThickness,
				PointGeometry = seri.PointGeometry,
			};
			var ls = seri as LineSeries;
			if(ls != null) {
				vm.Fill = ls.PointForeground;
				return vm;
			}
			return vm;
		}
	}

    /// <summary>残高推移を担うVM</summary>
	public class BalanceSeries : PathPeriodGraph {
		public BalanceSeries(GraphTabViewModel viewModel, Func<GraphTabViewModel, IEnumerable<object>> getSrc) : base(viewModel, getSrc) {
		}
		public override bool VisibilityMenu => true;
		public override double MaxLimit =>
			//this.CashFlowDrawPattern == BalanceCashFlow.Flow ? Math.Max(0d, Labels?.Count() - 1 ?? 0d) + 1.0 : 
			base.MaxLimit + 1.0;

		bool _visiblePl = true;
		public bool VisiblePL {
			get { return _visiblePl; }
			set {
				if (_visiblePl == value) return;
				_visiblePl = value;
				this.RaisePropertyChanged();
				this.Refresh();
			}
		}

		BalanceCashFlow _cfp = BalanceCashFlow.Flow;
		public BalanceCashFlow CashFlowDrawPattern {
			get { return _cfp; }
			set {
				if (_cfp == value) return;
				_cfp = value;
				this.RaisePropertyChanged();
				this.Refresh();
			}
		}
		protected override void Draw(IEnumerable<PlotValue> src) {
			var cls = Ext.BrushOrder();
			this.SeriesList.Add(new LineSeries() {
				Title = ViewModel.CurrentNode?.Name,
				Values = new ChartValues<double>(src.Select(a => a.Amount)),
				LineSmoothness = 0,
				Stroke = new SolidColorBrush(cls[0]),
				Fill = new SolidColorBrush(cls[0]) { Opacity = 0.1 },
			});
			switch (this.CashFlowDrawPattern) {
			case BalanceCashFlow.Flow:
				this.SeriesList.Add(new ColumnSeries() {
					Title = "キャッシュフロー",
					Values = new ChartValues<double>(src.Select(a=>a.Flow)),
					Stroke = new SolidColorBrush(cls[1]),
					Fill = new SolidColorBrush(cls[1]) { Opacity = 0.5 },
					StrokeThickness = 2,
					});
				break;
			case BalanceCashFlow.Stack:
				this.SeriesList.Add(new LineSeries() {
						Title = "累積キャッシュフロー",
						Values = new ChartValues<double>(src.Scan(0d,(ac,el)=>ac + el.Flow)),
						LineSmoothness = 0,
						Stroke = new SolidColorBrush(cls[1]),
						Fill = new SolidColorBrush(cls[1]) { Opacity = 0.1 },
						PointGeometry = DefaultGeometries.Square,
					});
				break;
			}
			if (this.VisiblePL) {
				var ssrc = src.Scan(new Tuple<double, double>(0, 0), (ac, el) =>
							   new Tuple<double, double>(ac.Item1 + el.Flow, el.Amount));
				this.SeriesList.Add(
					new LineSeries() {
						Title = "損益",
						Values = new ChartValues<double>(ssrc.Select(a=>a.Item2 - a.Item1)),
						LineSmoothness = 0,
						Stroke = new SolidColorBrush(cls[2]),
						Fill = new SolidColorBrush(cls[2]) { Opacity = 0.1 },
					});
			}
		}
	}
    /// <summary>指数推移を担うVM</summary>
	public class IndexGraphVm : PathPeriodGraph {
		
		public IndexGraphVm(GraphTabViewModel viewModel,Func<GraphTabViewModel,IEnumerable<object>> getSrc)
			: base(viewModel,getSrc) { }

		public override bool VisibilityMenu => true;

		protected override void Draw(IEnumerable<PlotValue> src) {
			var cls = Ext.BrushOrder();
			this.SeriesList.Add(
				new LineSeries() {
					Title = "基準価額(指数)",
					LineSmoothness = 0,
					Values = new ChartValues<double>(
						src.Select(a => a.Dietz + 1.0)
							.Scan(1d, (a, b) => a * b)
							.Select(a => a * 1000)),
					Stroke = new SolidColorBrush(cls[0]),
					Fill = new SolidColorBrush(cls[0]) { Opacity = 0.1 },
				});
		}
		protected override Func<double, string> NormalYFormatter => y => y.ToString("#,0.##");
	}
    /// <summary>ボラティリティ推移を担うVM</summary>
	public class VolatilityGraphVm : PathPeriodGraph {
		public VolatilityGraphVm(GraphTabViewModel viewModel,Func<GraphTabViewModel,IEnumerable<object>> getSrc)
			: base(viewModel,getSrc) { }

		protected override void Draw(IEnumerable<PlotValue> src) {
			var cls = Ext.BrushOrder();
			var dz = src.Select(a => a.Dietz).ToArray();
			this.SeriesList.Add(
				new LineSeries() {
					Title = "収益率(修正ディーツ法)",
					LineSmoothness = 0,
					Values = new ChartValues<double>(src.Select(a => (a.Dietz))),
					Stroke = new SolidColorBrush(cls[0]),
					Fill = new SolidColorBrush(cls[0]) { Opacity = 0.1 },
				});
		}
		protected override Func<double, string> NormalYFormatter => y => y.ToString("0.00%");
	}
}
