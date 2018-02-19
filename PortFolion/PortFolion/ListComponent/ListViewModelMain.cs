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
        /// <summary>現在の日付</summary>
		public DateTime? CurrentDate {
			get { return controler.CurrentDate; }
			set { controler.CurrentDate = value; }
		}
		bool _isTreeLoading = false;
		public bool IsTreeLoading {
			get { return _isTreeLoading; }
			set {
				if (_isTreeLoading == value) return;
				_isTreeLoading = value;
				RaisePropertyChanged();
				App.DoEvent();
			}
		}
		bool _isHistoryLoading = false;
		public bool IsHistoryLoading {
			get { return _isHistoryLoading; }
			set {
				if (_isHistoryLoading == value) return;
				_isHistoryLoading = value;
				RaisePropertyChanged();
				App.DoEvent();
			}
		}
        /// <summary>ロケーションツリーのルートを示す。</summary>
		public ObservableCollection<CommonNodeVM> Root { get; } = new ObservableCollection<CommonNodeVM>();
		void SetRoot(TotalRiskFundNode root) {
			if (Root.Any(a => a.Model == root)) return;
			Root.ForEach(r => {
				r.ReCalcurated -= refreshHistory;
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
				rt.ReCalcurated += refreshHistory;
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
        /// <summary>現在の日付</summary>
		public IEnumerable<string> Path {
			get { return controler.Path; }
			set { controler.Path = value; }
		}
		void setPath(IEnumerable<string> path) => this.Path = path;
        /// <summary>履歴データを更新する。</summary>
        /// <param name="path">履歴データのパス</param>
		public void RefreshHistory(IEnumerable<string> path) {
			IsHistoryLoading = true;
			_history =  CommonNodeVM.ReCalcHistory(path);
			IsHistoryLoading = false;
			this.RaisePropertyChanged(nameof(History));
		}
        /// <summary>
        /// イベントハンドラ登録用の履歴データ更新メソッド。
        /// 現在のロケーションツリーを再計算した後、履歴を更新する。
        /// </summary>
		void refreshHistory(CommonNodeVM src) {
			IsTreeLoading = true;
			if (this.CurrentDate != null) {
				CommonNodeVM.ReCalcurate(src);
			}
			IsTreeLoading = false;
			IsHistoryLoading = true;
			_history = CommonNodeVM.ReCalcHistory(this.Path);
			IsHistoryLoading = false;
			this.RaisePropertyChanged(nameof(History));
		}
		IEnumerable<VmCoreBase> _history = null;
		public IEnumerable<VmCoreBase> History
			=> _history;

		#region 現在の日付が変更された時の挙動
		string _selectedDateText = DateTime.Today.ToShortDateString();
		public string SelectedDateText {
			get { return _selectedDateText; }
			set {
				if (_selectedDateText == value) return;
				_selectedDateText = value;
				RaisePropertyChanged();
				AddNewRootCommand.RaiseCanExecuteChanged();
			}
		}
		ViewModelCommand addNewRootCommand;
		public ViewModelCommand AddNewRootCommand {
			get {
				if(addNewRootCommand == null) {
					addNewRootCommand = new ViewModelCommand(() => {
						var r = ResultWithValue.Of<DateTime>(DateTime.TryParse, _selectedDateText);
						if (!r) return;
						var rt = RootCollection.GetOrCreate(r.Value);
						this.CurrentDate = rt.CurrentDate;
					},()=>ResultWithValue.Of<DateTime>(DateTime.TryParse,_selectedDateText).Result);
				}
				return addNewRootCommand;
			}
		}
		

		DateTreeRoot dtr = new DateTreeRoot();
		public ObservableCollection<DateTree> DateList => dtr.Children;

		#endregion

		#region ロケーションツリーに対する操作
		public async void ApplyCurrentPerPrice() {
			IsTreeLoading = true;
			var acs = this.Root.FirstOrDefault()?
				.Levelorder().Select(a => a.Model)
				.Where(a => a.GetNodeType() == IO.NodeType.Account)
				.OfType<AccountNode>();
			if (acs == null || !acs.Any() || this.CurrentDate == null) return;
			var acse = acs.Select(a => new AccountEditVM(a));
			var lstLg = new List<string>();
			foreach(var a in acse) {
				lstLg.AddRange(await a.ApplyPerPrice());
				a.Apply.Execute(null);
			}
			IO.HistoryIO.SaveRoots((DateTime)this.CurrentDate);
			CommonNodeVM.ReCalcurate(this.Root.First());
			IsTreeLoading = false;
			if (lstLg.Any()) {
				string msg = "以下の銘柄は値を更新できませんでした。";
				var m = lstLg.Distinct().Aggregate(msg, (seed, ele) => seed + "\n" + ele);
				MessageBox.Show(m, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
			}else {
				MessageBox.Show("株価単価を更新しました。", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// <summary>ListViewModelを操作するためのインナークラス</summary>
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
