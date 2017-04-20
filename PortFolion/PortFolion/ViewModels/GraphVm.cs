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
				_mng.Update();
				this.BrakeDown.Update();
				foreach(var g in this.Graphs) {
					g.Update(_mng.GraphData);
				}
			};

			_mng = new gvMng(this);

			BrakeDown = new BrakeDownChart(this);
			this.CurrentNode = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= DateTime.Today)
							?? RootCollection.Instance.FirstOrDefault(a => DateTime.Today <= a.CurrentDate);






		}
		gvMng _mng;

		public void Refresh() {
			_mng.Refresh();
			BrakeDown.Refresh();
			foreach (var g in Graphs) g.Refresh(_mng.GraphData);
		}


		DateTreeRoot dtr = new DateTreeRoot();
		public ObservableCollection<DateTree> DateList => dtr.Children;

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
					if (CurrentNode != null) {
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
					setRoot(value?.Root() as TotalRiskFundNode);
				var prvPath = _commonNode?.Path ?? Enumerable.Empty<string>();
				var curPath = value?.Path ?? Enumerable.Empty<string>();
				_commonNode = value;
				CurrentPath = value?.Path ?? Enumerable.Empty<string>();
				RaisePropertyChanged(() => TargetLevel);
				RaisePropertyChanged();
				//RefreshBrakeDownList();
				//if (!prvPath.SequenceEqual(curPath)) {
					//RefreshHistoryList();
				//}
			}
		}
		IEnumerable<string> _currentPath = Enumerable.Empty<string>();
		public IEnumerable<string> CurrentPath {
			get { return _currentPath; }
			private set {
				if (_currentPath.SequenceEqual(value)) return;
				_currentPath = value;
				_mng.Update();
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
				_mng.Update();
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

		public ObservableCollection<GraphSeriesBase> Graphs = new ObservableCollection<GraphSeriesBase>();

		#region graphs
		TransitionSeries ts;
		public bool VisibleTransition {
			get { return !(ts == null || ts.IsDisposed); }
			set {
				if (value) {
					if (!VisibleTransition) {
						ts = new TransitionSeries(this);
						//Graphs.Insert(0, ts);
						Graphs.Add(ts);
						ts.Disposed += (o, e) => {
							Graphs.Remove(ts);
							RaisePropertyChanged();
						};
						//ts.Refresh(src);
						ts.Refresh(this._mng.GraphData);
					}
				} else {
					ts?.Dispose();
				}
			}
		}

		TransitionStackCFSeries tscfs;
		public bool VisibleTransitionStackCF {
			get { return !(tscfs == null || tscfs.IsDisposed); }
			set {
				if (!value) {
					tscfs.Dispose();
				}else if(value && !VisibleTransitionStackCF) {
					tscfs = new TransitionStackCFSeries(this);
					Graphs.Insert(0, tscfs);
					tscfs.Disposed += (o, e) => {
						Graphs.Remove(tscfs);
						RaisePropertyChanged();
					};
					//Refresh
					tscfs.Refresh(this._mng.GraphData);
				}
			}
		}

		TransitionPLSeries tpls;
		public bool VisibleTransitionPL {
			get { return !(tpls == null || tpls.IsDisposed); }
			set {
				if (!value) {
					tpls.Dispose();
				}else if(value && !VisibleTransitionPL) {
					tpls = new TransitionPLSeries(this);
					Graphs.Insert(0, tpls);
					tpls.Disposed += (o, e) => {
						Graphs.Remove(tpls);
						RaisePropertyChanged();
					};
					tpls.Refresh(_mng.GraphData);
				}
			}
		}

		#endregion

		class gvMng {
			GraphTabViewModel gtvm;
			IEnumerable<string> _curPath = Enumerable.Empty<string>();
			Period _period;

			public gvMng(GraphTabViewModel vm) {
				gtvm = vm;
				Refresh();
			}
			public void Update() {
				if(!_curPath.SequenceEqual(gtvm.CurrentPath) || _period != gtvm.TimePeriod) {
					this.Refresh();
				}
			}
			public void Refresh() {
				_curPath = gtvm.CurrentPath;
				_period = gtvm.TimePeriod;
				GraphData = RootCollection
					.GetNodeLine(_curPath)
					.ToGraphValues(_period);
			}
			public IEnumerable<GraphValue> GraphData { get; private set; }
		}
	}
	public class BrakeDownChart : SeriesCollection {
		
		int _targetLv;
		DividePattern _divide;
		CommonNode _curNode;

		GraphTabViewModel _vm;

		public BrakeDownChart(GraphTabViewModel viewModel) {
			_vm = viewModel;
			var cmp = Mappers.Pie<TempValue>().Value(tv => tv.Amount);
			Charting.For<TempValue>(cmp);
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
			var tgnss = _curNode.MargeNodes(_targetLv, _divide).ToArray();
			tgnss.Zip(Ext.BrushColors().Repeat(), (a, b) => new { Data = a, Brush = b })
				.ForEach(a => {
					a.Data.Fill = new SolidColorBrush(a.Brush);
					a.Data.Stroke = new SolidColorBrush(Color.Multiply(a.Brush, 0.5f));
					a.Data.StrokeThickness = 5;
					var ps = new PieSeries() {
						Title = a.Data.Title,
						Values = new ChartValues<TempValue>() { a.Data },
						DataLabels = true,
						LabelPoint = cp => a.Data.Title,
						LabelPosition = PieLabelPosition.OutsideSlice,
						StrokeThickness = a.Data.StrokeThickness,
						Fill = a.Data.Fill,
						Stroke = a.Data.Stroke,
					};
					a.Data.PointGeometry = ps.PointGeometry;
					this.Add(ps);
				});
			this.BrakeDownLegend = tgnss;
		}
		IEnumerable<TempValue> _legend;
		public IEnumerable<TempValue> BrakeDownLegend {
			get { return _legend; }
			private set {
				if (_legend == value) return;
				_legend = value;
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(BrakeDownLegend)));
			}
		}
	}

	public abstract class GraphSeriesBase : SeriesCollection , IDisposable {
		protected GraphTabViewModel ViewModel { get; }

		public GraphSeriesBase(GraphTabViewModel viewModel) {
			ViewModel = viewModel;
			RangeChangedCmd = new ViewModelCommand(rangeChanged);
		}
		/// <summary>変更があった場合更新する</summary>
		public abstract void Update(IEnumerable<GraphValue> src);
		/// <summary>現在の条件で再描画する</summary>
		public abstract void Refresh(IEnumerable<GraphValue> src);
		protected void RemoveAll() {
			var cnt = this.Count;
			while (0 < cnt) {
				cnt--;
				try {
					this.RemoveAt(cnt);
				} catch {

				}
			}
		}
		IEnumerable<string> _labels = Enumerable.Empty<string>();
		public IEnumerable<string> Labels {
			get { return _labels; }
			set {
				if (_labels.SequenceEqual(value)) return;
				_labels = value;
				base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Labels)));
			}
		}
		double min = double.NaN;
		public double DisplayMinValue {
			get { return min; }
			set {
				if (min == value) return;
				min = value;
				base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(DisplayMinValue)));
			}
		}
		double max = double.NaN;
		public double DisplayMaxValue {
			get { return max; }
			set {
				if (max == value) return;
				max = value;
				base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(DisplayMaxValue)));
			}
		}
		public double MaxLimit => Math.Max(0d, Labels?.Count() -1 ?? 0d);
		//public Func<double,string> XFormatter { get; set; }
		public virtual Func<double, string> YFormatter => y => y.ToString("#,0.#");

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
		public bool IsDisposed { get; private set; }
		public void Dispose() {
			if (IsDisposed) return;
			this.RemoveAll();
			IsDisposed = true;
			Disposed?.Invoke(this, new EventArgs());
		}
		public event EventHandler Disposed;
	}

	public class TransitionSeries : GraphSeriesBase {

		IEnumerable<string> _curPath;
		Period _period;

		public TransitionSeries(GraphTabViewModel viewModel) : base(viewModel) {
			//YFormatter = y => y.ToString("#,0.#");
		}
		public override void Update(IEnumerable<GraphValue> src) {
			if(!_curPath.SequenceEqual(ViewModel.CurrentPath) || _period != ViewModel.TimePeriod) {
				Refresh(src);
			}
		}
		public override void Refresh(IEnumerable<GraphValue> src) {
			_curPath = ViewModel.CurrentPath;
			_period = ViewModel.TimePeriod;

			RemoveAll();

			Labels = src.Select(a => a.Date.ToString("yyyy/M/d")).ToArray();

			Draw(src);
		}
		protected virtual void Draw(IEnumerable<GraphValue> src) {
			this.Add(new LineSeries() {
				Title = ViewModel.CurrentNode?.Name,
				Values = new ChartValues<double>(src.Select(a => a.Amount)),
				LineSmoothness = 0,
			});
			this.Add(new ColumnSeries() {
				Title = "キャッシュフロー",
				Values = new ChartValues<double>(src.Select(a=>a.Flow)),
			});
		}
	}

	public class TransitionStackCFSeries : TransitionSeries {
		public TransitionStackCFSeries(GraphTabViewModel viewModel) : base(viewModel) {
		}
		protected override void Draw(IEnumerable<GraphValue> src) {
			//var ssrc = src.Scan(new Tuple<double, double>(0, 0), (ac, el) =>
			//				   new Tuple<double, double>(ac.Item1 + el.Flow, el.Amount));
			this.Add(
				new LineSeries() {
					Title = ViewModel.CurrentNode?.Name,
					Values = new ChartValues<double>(src.Select(a=>a.Amount)),
					LineSmoothness = 0,
				});
			this.Add(
				new LineSeries() {
					Title = "累積キャッシュフロー",
					Values = new ChartValues<double>(src.Scan(0d,(ac,el)=>ac + el.Flow)),
					LineSmoothness = 0,
				});
		}
	}
	public class TransitionPLSeries : TransitionSeries {
		public TransitionPLSeries(GraphTabViewModel viewModel) : base(viewModel) { }
		protected override void Draw(IEnumerable<GraphValue> src) {
			
			var ssrc = src.Scan(new Tuple<double, double>(0, 0), (ac, el) =>
							   new Tuple<double, double>(ac.Item1 + el.Flow, el.Amount));
			this.Add(
				new LineSeries() {
					Title = "損益",
					Values = new ChartValues<double>(ssrc.Select(a=>a.Item2 - a.Item1)),
					LineSmoothness = 0,
				});
		}
	}
}
