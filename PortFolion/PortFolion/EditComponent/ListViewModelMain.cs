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
using System.Windows;
using System.Windows.Threading;

namespace PortFolion.ViewModels {
	public class ListviewModel : ViewModel {
		
		VmControler controler;
		public ListviewModel() {
			controler = new VmControler(this);
			controler.CurrentDate = DateTime.Today;
			this.ExpandAllNode();
			controler.PropertyChanged += (o, e) => RaisePropertyChanged(e.PropertyName);
			this.CompositeDisposable.Add(new CollectionChangedWeakEventListener(RootCollection.Instance, controler.RootCollectionChanged));
			var d = new LivetWeakEventListener<EventHandler<DateTimeSelectedEventArgs>, DateTimeSelectedEventArgs>(
				h => h,
				h => dtr.DateTimeSelected += h,
				h => dtr.DateTimeSelected -= h,
				(s, e) => this.CurrentDate = e.SelectedDateTime);
			this.CompositeDisposable.Add(d);
		}
		public DateTime? CurrentDate {
			get { return controler.CurrentDate; }
			set { controler.CurrentDate = value; }
		}
		bool _isTreeLoading = false;
		public bool IsTreeLoading {
			get { return _isTreeLoading; }
			private set {
				if (_isTreeLoading == value) return;
				_isTreeLoading = value;
				RaisePropertyChanged();
				App.DoEvent();
			}
		}
		bool _isHistoryLoading = false;
		public bool IsHistoryLoading {
			get { return _isHistoryLoading; }
			private set {
				if (_isHistoryLoading == value) return;
				_isHistoryLoading = value;
				RaisePropertyChanged();
				App.DoEvent();
			}
		}
		//private void DoEvents(){
		//	DispatcherFrame frame = new DispatcherFrame();
		//	var callback = new DispatcherOperationCallback(obj =>
		//	{
		//		((DispatcherFrame)obj).Continue = false;
		//		return null;
		//	});
		//	Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
		//	Dispatcher.PushFrame(frame);
		//}
		public ObservableCollection<CommonNodeVM> Root { get; } = new ObservableCollection<CommonNodeVM>();
		void SetRoot(TotalRiskFundNode root) {
			if (Root.Any(a => a.Model == root)) return;
			Root.ForEach(r => {
				r.ReCalcurated -= RefreshHistory;
				r.SetPath -= setPath;
			});

			List<NodePath<string>> expns = new List<NodePath<string>>();
			if (Root.Any()) {
				var ls = Root.First().Preorder().Where(a => a.IsExpand).Select(a => a.Path);
				expns.AddRange(ls);
			}
			Root.Clear();
			if (root != null) {
				var rt = CommonNodeVM.Create(root);
				rt.ReCalcurated += RefreshHistory;
				rt.SetPath += setPath;
				if (rt.CurrentDate != null) {
					IsTreeLoading = true;
					CommonNodeVM.ReCalcurate(rt);
					IsTreeLoading = false;
				}
				Root.Add(rt);
				rt.Preorder()
					.Where(a => expns.Any(b => b.SequenceEqual(a.Path)))
					.ForEach(a => a.IsExpand = true);
			}
		}
		public IEnumerable<string> Path {
			get { return controler.Path; }
			set { controler.Path = value; }
		}
		void setPath(IEnumerable<string> path) => this.Path = path;
		public void RefreshHistory(IEnumerable<string> path) {
			//_history = CommonNodeVM.ReCalcHistory(path);
			this.IsHistoryLoading = true;
			_history = CommonNodeVM.ReCalcHistory(path);
			this.IsHistoryLoading = false;
			this.RaisePropertyChanged(nameof(History));
		}
		public void RefreshHistory(CommonNodeVM src) {

			IsHistoryLoading = true;
			IsTreeLoading = true;
			if (this.CurrentDate != null) {
				CommonNodeVM.ReCalcurate(src);
			}
			IsTreeLoading = false;

			_history = CommonNodeVM.ReCalcHistory(this.Path);
			IsHistoryLoading = false;

			this.RaisePropertyChanged(nameof(History));
		}
		IEnumerable<VmCoreBase> _history = null;
		public IEnumerable<VmCoreBase> History
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
			this.CurrentDate = d;
			//dtr.SelectAt(d);
			//this.SetCurrentDate(d);
		},()=> {
			var d = ResultWithValue.Of<DateTime>(DateTime.TryParse, _selectedDateText);
			return d.Result;//&& !RootCollection.Instance.ContainsKey(d.Value);//.Any(a=>a.CurrentDate != d.Value);
		});

		DateTreeRoot dtr = new DateTreeRoot();
		public ObservableCollection<DateTree> DateList => dtr.Children;

		#endregion

		#region tree menu
		public void ApplyCurrentPerPrice() {
			IsTreeLoading = true;
			var acs = this.Root.FirstOrDefault()?
				.Levelorder().Select(a => a.Model)
				.Where(a => a.GetNodeType() == IO.NodeType.Account)
				.OfType<AccountNode>();
			if (acs == null || !acs.Any() || this.CurrentDate == null) return;
			var acse = acs.Select(a => new AccountEditVM(a));
			var lstLg = new List<string>();
			foreach(var a in acse) {
				lstLg.AddRange(a.ApplyPerPrice());
				a.Apply.Execute(null);
			}
			IO.HistoryIO.SaveRoots((DateTime)this.CurrentDate);
			CommonNodeVM.ReCalcurate(this.Root.First());
			IsTreeLoading = false;
			if (lstLg.Any()) {
				string msg = "以下の銘柄は値を更新できませんでした。";
				var m = lstLg.Distinct().Aggregate(msg, (seed, ele) => seed + "\n" + ele);
				MessageBox.Show(m, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			acse.ForEach(a => a.Dispose());
		}
		public void DeleteCurrentDate() {
			if (CurrentDate == null || RootCollection.Instance.IsEmpty()) return;
			Action delete = () => {
				var d = (DateTime)CurrentDate;
				RootCollection.Instance.Remove(this.Root.First().Model as TotalRiskFundNode);
				IO.HistoryIO.SaveRoots(d);
			};
			if (RootCollection.Instance.Last().CurrentDate == CurrentDate) {
				if (!this.Root.First().Model.HasTrading) {
					delete();
				}else {
					if(MessageBoxResult.OK == MessageBox.Show("取引情報が含まれています。削除しますか？","Notice",MessageBoxButton.OKCancel,MessageBoxImage.Exclamation,MessageBoxResult.Cancel)) {
						delete();
					}
				}
			}else if (!this.Root.First().Model.HasTrading) {
				if(MessageBoxResult.OK == MessageBox.Show(((DateTime)CurrentDate).ToString("yyyy年M月d日") + "の書込みデータを削除します。","Notice",MessageBoxButton.OKCancel,MessageBoxImage.Information,MessageBoxResult.Cancel)) {
					delete();
				}
			}else {
				MessageBox.Show("取引情報が含まれている過去のデータは削除できません。", "Notice", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}
		public void ExpandCurrentNode() {
			if (!this.Path.Any()) return;
			var c = Root.SelectMany(a => a.Levelorder()).FirstOrDefault(a => a.Path.SequenceEqual(this.Path));
			if (c != null)
				foreach (var n in c.Upstream()) n.IsExpand = true;
		}
		public void ExpandAllNode() {
			if (Root.Any())
				foreach (var n in Root.SelectMany(a => a.Levelorder()))
					n.IsExpand = true;
		}
		public void CloseAllNode() {
			if (Root.Any())
				foreach (var n in Root.SelectMany(a => a.Levelorder()))
					n.IsExpand = false;
		}
		#endregion

		#region Controler as inner class
		private class VmControler : NotificationObject {
			ListviewModel lvm;
			public VmControler(ListviewModel vm) {
				lvm = vm;
			}

			public void RootCollectionChanged(object s, NotifyCollectionChangedEventArgs e) {
				lvm.dtr.Refresh();
				TotalRiskFundNode rt;
				switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					rt = e.NewItems.OfType<TotalRiskFundNode>().FirstOrDefault();
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
				lvm.RefreshHistory(this.Path);
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
					var r = RootCollection.Instance.FirstOrDefault(a => value <= a.CurrentDate)
						?? RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= value);
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
						Path = r.SearchNodeOf(Path).Path;
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
					//if (_path.SequenceEqual(value)) return;
					var pt = value.ToArray();
					if (_path.SequenceEqual(pt)) return;
					_path = pt;
					RaisePropertyChanged();
					if (_path.Any()) {
						//lvm.ExpandCurrentNode();
						lvm.RefreshHistory(_path);
					}
				}
			}
		}
		#endregion
	}
}
