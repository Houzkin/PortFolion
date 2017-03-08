using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using Livet.Commands;
using PortFolion.Core;
using Houzkin.Tree;
using Houzkin.Architecture;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Windows.Input;
using Livet.EventListeners.WeakEvents;
using Houzkin;

namespace PortFolion.ViewModels {
	public class ListviewModel : ViewModel {
		
		VmControler controler;
		public ListviewModel() {
			controler = new VmControler(this);
			controler.PropertyChanged += (o, e) => RaisePropertyChanged(e.PropertyName);
			this.CompositeDisposable.Add(new CollectionChangedWeakEventListener(RootCollection.Instance, controler.RootCollectionChanged));
		}

		//private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
		//	dtr.Refresh();
		//	RaisePropertyChanged(nameof(DateList));
		//	TotalRiskFundNode rt;
		//	switch (e.Action) {
		//	case NotifyCollectionChangedAction.Add:
		//		rt = e.NewItems[0] as TotalRiskFundNode;
		//		if (rt == null) goto default;
		//		break;
		//	default:
		//		rt = Model.LastOrDefault(a => a.CurrentDate <= (this.CurrentDate ?? DateTime.Today)) ?? Model.LastOrDefault();
		//		break;
		//	}
		//	if (totalRiskFund == rt) return;
		//	if(rt == null) {
		//		totalRiskFund = null;
		//		CurrentDate = null;// DateTime.Today;
		//		Path = Enumerable.Empty<string>();
		//		Refresh();
		//	}else {
		//		totalRiskFund = rt;
		//		SetCurrentDate(totalRiskFund.CurrentDate);
		//	}
		//}

		public DateTime? CurrentDate {
			get { return controler.CurrentDate; }
			set { controler.CurrentDate = value; }
		}
		public ObservableCollection<CommonNodeVM> Root { get; } = new ObservableCollection<CommonNodeVM>();
		void SetRoot(TotalRiskFundNode root) {
			Root.Clear();
			if (root != null) {
				Root.Add(CommonNodeVM.Create(root));
				this.ExpandCurrentNode();
			}
		}

		public IEnumerable<string> Path {
			get { return controler.Path; }
			set { controler.Path = value; }
		}

		public void RefreshHistory() {
			_history = RootCollection.GetNodeLine(new NodePath<string>(Path)).Select(a => CommonNodeVM.Create(a.Value));
			this.RaisePropertyChanged(nameof(History));
		}
		IEnumerable<CommonNodeVM> _history = null;
		public IEnumerable<CommonNodeVM> History
			=> _history;
		
		#region date
		string _selectedDateText = DateTime.Today.ToShortDateString();
		public string SelectedDateText {
			get { return _selectedDateText; }
			set {
				if (_selectedDateText == value) return;
				_selectedDateText = value;
				RaisePropertyChanged();
				addNewRootCommand.RaiseCanExecuteChanged();
			}
		}
		ListenerCommand<DateTime> addNewRootCommand;
		public ICommand AddNewRootCommand => addNewRootCommand = new ListenerCommand<DateTime>(d => {
			var r = RootCollection.GetOrCreate(d);
			if (string.IsNullOrEmpty(r.Name)) r.Name = "総リスク資産";
			//dtr.SelectAt(d);
			//this.SetCurrentDate(d);
		},()=> {
			var d = ResultWithValue.Of<DateTime>(DateTime.TryParse, _selectedDateText);
			return d.Result;
		});

		DateTreeRoot dtr = new DateTreeRoot();
		public ObservableCollection<DateTree> DateList => dtr.Children;
		
		#endregion

		#region tree
		public void ExpandCurrentNode() {
			if (!this.Path.Any()) return;
			var c = Root.SelectMany(a=>a.Levelorder()).FirstOrDefault(a => a.Path.SequenceEqual(this.Path));
			if(c != null)
				foreach (var n in c.Upstream()) n.IsExpand = true;
		}
		public void ExpandAllNode() {
			if (Root.Any())
				foreach (var n in Root.SelectMany(a=>a.Levelorder()))
					n.IsExpand = true;
		}
		public void CloseAllNode() {
			if (Root.Any())
				foreach (var n in Root.SelectMany(a=>a.Levelorder()))
					n.IsExpand = false;
		}
		#endregion

		#region Controler as inner class
		private class VmControler : NotificationObject {
			ListviewModel lvm;
			public VmControler(ListviewModel vm) {
				lvm = vm;
				//RootCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				this.CurrentDate = DateTime.Today;
			}

			public void RootCollectionChanged(object s, NotifyCollectionChangedEventArgs e) {
				lvm.dtr.Refresh();
				TotalRiskFundNode rt;
				switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					rt = e.NewItems.Cast<TotalRiskFundNode>().FirstOrDefault();// as TotalRiskFundNode;
					if (rt == null) goto default;
					lvm.SetRoot(rt);
					break;
				default:
					var d = CurrentDate ?? DateTime.Today;
					rt = RootCollection.Instance.FirstOrDefault(a => d <= a.CurrentDate)
						?? RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= d);
					break;
				}
				if (rt == null) this.CurrentDate = null;
				else this.CurrentDate = rt.CurrentDate;
			}

			DateTime? _currentDate;
			public DateTime? CurrentDate {
				get { return _currentDate; }
				set {
					if (_currentDate == value) return;
					if (value == null) {
						_currentDate = null;
						RaisePropertyChanged();
						lvm.SetRoot(null);
						return;
					}
					var r = RootCollection.Instance.FirstOrDefault(a => _currentDate <= a.CurrentDate)
						?? RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= _currentDate);
					if (r == null) {
						_currentDate = null;
						RaisePropertyChanged();
						lvm.SetRoot(null);
						return;
					}
					_currentDate = r.CurrentDate;
					RaisePropertyChanged();
					lvm.SetRoot(r);
					lvm.dtr.SelectAt(r.CurrentDate);

					if (Path.Any()) {
						Path = r.SearchNodeOf(Path)?.Path ?? r.Path;
					}else {
						Path = r.Path;
					}
				}
			}

			IEnumerable<string> _path = Enumerable.Empty<string>();
			public IEnumerable<string> Path {
				get { return _path; }
				set {
					value = value ?? Enumerable.Empty<string>();
					if (_path.SequenceEqual(value)) return;
					_path = value;
					RaisePropertyChanged();
					if (_path.Any()) {
						lvm.ExpandCurrentNode();
						lvm.RefreshHistory();
					}
				}
			}
		}
		#endregion
	}
}
