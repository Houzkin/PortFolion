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
	public class GraphTabViewModel : ViewModel {

		public GraphTabViewModel() {
			this.CurrentNode = RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= DateTime.Today)
							?? RootCollection.Instance.FirstOrDefault(a => DateTime.Today <= a.CurrentDate);

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
		}

		public void Refresh() { }

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
				if (!_currentPath.SequenceEqual(value)) return;
				_currentPath = value;
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

		public BrakeDownList BrakeDown { get; }

		public ObservableCollection<DisplaySeriesCollection> Graphs = new ObservableCollection<DisplaySeriesCollection>();

	}
}
