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
using Livet.Messaging;

namespace PortFolion.ViewModels {
	
	public class EditViewModel : ViewModel {
		#region static methods
		static EditViewModel _instance;
		public static EditViewModel Instance
			=> _instance = _instance ?? new EditViewModel();
		#endregion
		VmControler controler;
		private EditViewModel() {
			this.CompositeDisposable.Add(() => EditEndAsCancel());
			this.CompositeDisposable.Add(() => _instance = null);
			controler = new VmControler(this);
			controler.SetCurrentDate(DateTime.Today);
			this.ExpandAllNode();
			controler.PropertyChanged += (o, e) => RaisePropertyChanged(e.PropertyName);
			this.CompositeDisposable.Add(new CollectionChangedWeakEventListener(RootCollection.Instance, controler.RootCollectionChanged));
			var d = new LivetWeakEventListener<EventHandler<DateTimeSelectedEventArgs>, DateTimeSelectedEventArgs>(
				h => h,
				h => dtr.DateTimeSelected += h,
				h => dtr.DateTimeSelected -= h,
				(s, e) => this.CurrentDate = e.SelectedDateTime);
			this.CompositeDisposable.Add(d);
			this.History = new HistoryViewModel();
			//this.History.OpenCloseOrder += isOpen => {
			//	if (isOpen)
			//		this.Messenger.Raise(
			//			new InteractionMessage("OpenHistoryFlyout"));
			//	else
			//		this.Messenger.Raise(
			//			new InteractionMessage("CloseHistoryFlyout"));
			//};
		}
		/// <summary>現在の日付</summary>
		DateTime? _currentDate;
		public DateTime? CurrentDate {
			get => _currentDate;
			set{
				if (_currentDate == value) return;
				_currentDate = value;
				this.RaisePropertyChanged();
				controler.SetCurrentDate(value);
			}
		}
		private void setCurrentDate(DateTime? date){
			if (_currentDate == date) return;
			_currentDate = date; 
			this.RaisePropertyChanged(nameof(CurrentDate));
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

		IEnumerable<string> _path = Enumerable.Empty<string>();
		/// <summary>現在の日付</summary>
		public IEnumerable<string> Path {
			get{ return _path; }
			set{
				if (_path.SequenceEqual(value)) return;
				_path = value;
				this.RaisePropertyChanged();
				controler.RefreshHistory(value,false);
			}
		}
		bool setPath(IEnumerable<string> path){
			if (_path.SequenceEqual(path)) return false;
			_path = path;
			RaisePropertyChanged(nameof(Path));
			return true;
		}

		public HistoryViewModel History { get; }
		private void setHistory(IEnumerable<string> path,bool open = false){
			History.Refresh(path, open);
			RaisePropertyChanged(nameof(History));
		}

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
				if (addNewRootCommand == null) {
					addNewRootCommand = new ViewModelCommand(() => {
						var r = ResultWithValue.Of<DateTime>(DateTime.TryParse, _selectedDateText);
						if (!r) return;
						var rt = RootCollection.GetOrCreate(r.Value);
						this.CurrentDate = rt.CurrentDate;
					}, () => ResultWithValue.Of<DateTime>(DateTime.TryParse, _selectedDateText).Result);
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
			var lstLg = new List<string>();
			//var acse = acs.Select(a => new AccountEditVM(a));
			//foreach (var a in acse) {
			//	lstLg.AddRange(await a.ApplyPerPrice());
			//	a.Apply.Execute(null);
			//}
			IO.HistoryIO.SaveRoots((DateTime)this.CurrentDate);
			CommonNodeVM.ReCalcurate(this.Root.First());
			controler.RefreshHistory(this.Path,false);
			IsTreeLoading = false;
			if (lstLg.Any()) {
				string msg = "以下の銘柄は値を更新できませんでした。";
				var m = lstLg.Distinct().Aggregate(msg, (seed, ele) => seed + "\n" + ele);
				MessageBox.Show(m, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
			} else {
				MessageBox.Show("株価単価を更新しました。", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			//acse.ForEach(a => a.Dispose());
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
				}
				else {
					if (MessageBoxResult.OK == MessageBox.Show("取引情報が含まれています。削除しますか？", "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel)) {
						delete();
					}
				}
			}
			else if (!this.Root.First().Model.HasTrading) {
				if (MessageBoxResult.OK == MessageBox.Show(((DateTime)CurrentDate).ToString("yyyy年M月d日") + "の書込みデータを削除します。", "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.Cancel)) {
					delete();
				}
			}
			else {
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
		#region 終了処理
		HashSet<TotalRiskFundNode> _EditSet = new HashSet<TotalRiskFundNode>();
		public void AddEditList(IEnumerable<CommonNode> nodes) {
			nodes.Select(a => a.Root())
				.OfType<TotalRiskFundNode>()
				.ForEach(a => _EditSet.Add(a));
		}
		public void AddEditList(CommonNode node) {
			this.AddEditList(new CommonNode[] { node });
		}
		public void EditEndAsExecute() {
			if (_EditSet.IsEmpty()) return;
			IO.HistoryIO.SaveRoots(_EditSet.Min(a => a.CurrentDate), _EditSet.Max(a => a.CurrentDate));
			_EditSet.Clear();
		}
		public void EditEndAsCancel() {
			if (_EditSet.IsEmpty()) return;
			RootCollection.Instance.Refresh();
			_EditSet.Clear();
		}
		#endregion
		#region Controler as inner class
		/// <summary>ListViewModelを操作するためのインナークラス</summary>
		private class VmControler : NotificationObject {
			EditViewModel lvm;
			public VmControler(EditViewModel vm) {
				lvm = vm;
			}

			public void RootCollectionChanged(object s, NotifyCollectionChangedEventArgs e) {
				lvm.dtr.Refresh();
				TotalRiskFundNode rt;
				switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					rt = e.NewItems.OfType<TotalRiskFundNode>().FirstOrDefault();
					if (rt == null) goto default;
					SetRoot(rt);
					break;
				default:
					var d = lvm.CurrentDate ?? DateTime.Today;
					rt = RootCollection.Instance.FirstOrDefault(a => d <= a.CurrentDate)
						?? RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= d);
					break;
				}
				if (rt == null) SetCurrentDate(null);
				else SetCurrentDate(rt.CurrentDate);
				RefreshHistory(lvm.Path,false);
			}
			public void SetCurrentDate(DateTime? date){
				if(date == null){ 
					this.SetRoot(null);
					return;
				}
				var r = RootCollection.Instance.FirstOrDefault(a => date <= a.CurrentDate)
					?? RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= date);
				if(r == null){
					lvm.setCurrentDate(null);
					this.SetRoot(null);
					return;
				}
				this.SetRoot(r);
				lvm.dtr.SelectAt(r.CurrentDate);
				//if(lvm.Path.Any())
				//	lvm.setPath(r.SearchNodeOf(lvm.Path).Path);
				//else 
				//	lvm.setPath(r.Path); 
				var p = lvm.Path.Any() ? r.SearchNodeOf(lvm.Path).Path : r.Path;
				if (lvm.setPath(p)) lvm.History.Refresh(p, false);
			}
			public void SetRoot(TotalRiskFundNode root){
				if (lvm.Root.Any(a => a.Model == root)) return;
				lvm.Root.ForEach(r => {
					r.ReCalcurated -= RefreshHistory;
					r.SetPath -= DisplayHistory;
				});
				var expns = lvm.Root.FirstOrDefault()?.Preorder().Where(a => a.IsExpand).Select(a => a.Path).ToArray() ?? Enumerable.Empty<NodePath<string>>();
				lvm.Root.Clear();
				if (root == null) return;
				var rt = CommonNodeVM.Create(root);
				rt.ReCalcurated += RefreshHistory;
				rt.SetPath += DisplayHistory;
				if(rt.CurrentDate != null){
					lvm.IsTreeLoading = true;
					CommonNodeVM.ReCalcurate(rt);
					lvm.IsTreeLoading = false;
				}
				lvm.Root.Add(rt);
				rt.Preorder()
					.Where(a => expns.Any(b => b.SequenceEqual(a.Path)))
					.ForEach(a => a.IsExpand = true);
			}
			/// <summary>履歴を表示させる</summary>
			private void DisplayHistory(IEnumerable<string> path){ 
				if(lvm.setPath(path))
					this.RefreshHistory(path, true);
			}
			/// <summary>履歴データを更新する。</summary>
			/// <param name="path">履歴データのパス</param>
			/// <param name="open">openでなかった場合openするかどうか</param>
			public void RefreshHistory(IEnumerable<string> path,bool open = false){
				//lvm.setHistory(path,open);
				lvm.setPath(path);
				lvm.History.Refresh(path,open);
			}
			/// <summary>
			/// イベントハンドラ登録用の履歴データ更新メソッド。
			/// 現在のロケーションツリーを再計算した後、履歴を更新する。
			/// </summary>
			public void RefreshHistory(CommonNodeVM src){
				lvm.IsTreeLoading = true;
				if (lvm.CurrentDate != null) CommonNodeVM.ReCalcurate(src);
				lvm.IsTreeLoading = false;
				RefreshHistory(lvm.Path);
			}
		}
		#endregion
		#region backup
		//VmControler controler;
		//private EditViewModel() {
		//	this.CompositeDisposable.Add(() => EditEndAsCancel());
		//	this.CompositeDisposable.Add(() => _instance = null);
		//	controler = new VmControler(this);
		//	controler.CurrentDate = DateTime.Today;
		//	this.ExpandAllNode();
		//	controler.PropertyChanged += (o, e) => RaisePropertyChanged(e.PropertyName);
		//	this.CompositeDisposable.Add(new CollectionChangedWeakEventListener(RootCollection.Instance, controler.RootCollectionChanged));
		//	var d = new LivetWeakEventListener<EventHandler<DateTimeSelectedEventArgs>, DateTimeSelectedEventArgs>(
		//		h => h,
		//		h => dtr.DateTimeSelected += h,
		//		h => dtr.DateTimeSelected -= h,
		//		(s, e) => this.CurrentDate = e.SelectedDateTime);
		//	this.CompositeDisposable.Add(d);
		//}
		///// <summary>現在の日付</summary>
		//public DateTime? CurrentDate {
		//	get { return controler.CurrentDate; }
		//	set { controler.CurrentDate = value; }
		//}
		//bool _isTreeLoading = false;
		//public bool IsTreeLoading {
		//	get { return _isTreeLoading; }
		//	set {
		//		if (_isTreeLoading == value) return;
		//		_isTreeLoading = value;
		//		RaisePropertyChanged();
		//		App.DoEvent();
		//	}
		//}
		//bool _isHistoryLoading = false;
		//public bool IsHistoryLoading {
		//	get { return _isHistoryLoading; }
		//	set {
		//		if (_isHistoryLoading == value) return;
		//		_isHistoryLoading = value;
		//		RaisePropertyChanged();
		//		App.DoEvent();
		//	}
		//}
		///// <summary>ロケーションツリーのルートを示す。</summary>
		//public ObservableCollection<CommonNodeVM> Root { get; } = new ObservableCollection<CommonNodeVM>();
		//void SetRoot(TotalRiskFundNode root) {
		//	if (Root.Any(a => a.Model == root)) return;
		//	Root.ForEach(r => {
		//		r.ReCalcurated -= refreshHistory;
		//		r.SetPath -= setPath;
		//	});

		//	List<NodePath<string>> expns = new List<NodePath<string>>();
		//	if (Root.Any()) {
		//		var ls = Root.First().Preorder().Where(a => a.IsExpand).Select(a => a.Path);
		//		expns.AddRange(ls);
		//	}
		//	Root.Clear();
		//	if (root != null) {
		//		var rt = CommonNodeVM.Create(root);
		//		rt.ReCalcurated += refreshHistory;
		//		rt.SetPath += setPath;
		//		if (rt.CurrentDate != null) {
		//			IsTreeLoading = true;
		//			CommonNodeVM.ReCalcurate(rt);
		//			IsTreeLoading = false;
		//		}
		//		Root.Add(rt);
		//		rt.Preorder()
		//			.Where(a => expns.Any(b => b.SequenceEqual(a.Path)))
		//			.ForEach(a => a.IsExpand = true);
		//	}

		//}
		///// <summary>現在の日付</summary>
		//public IEnumerable<string> Path {
		//	get { return controler.Path; }
		//	set { controler.Path = value; }
		//}
		//void setPath(IEnumerable<string> path) => this.Path = path;
		///// <summary>履歴データを更新する。</summary>
		///// <param name="path">履歴データのパス</param>
		//public void RefreshHistory(IEnumerable<string> path) {
		//	IsHistoryLoading = true;
		//	_history = CommonNodeVM.ReCalcHistory(path);
		//	IsHistoryLoading = false;
		//	this.RaisePropertyChanged(nameof(History));
		//}
		///// <summary>
		///// イベントハンドラ登録用の履歴データ更新メソッド。
		///// 現在のロケーションツリーを再計算した後、履歴を更新する。
		///// </summary>
		//void refreshHistory(CommonNodeVM src) {
		//	IsTreeLoading = true;
		//	if (this.CurrentDate != null) {
		//		CommonNodeVM.ReCalcurate(src);
		//	}
		//	IsTreeLoading = false;
		//	IsHistoryLoading = true;
		//	_history = CommonNodeVM.ReCalcHistory(this.Path);
		//	IsHistoryLoading = false;
		//	this.RaisePropertyChanged(nameof(History));
		//}
		//IEnumerable<VmCoreBase> _history = null;
		//public IEnumerable<VmCoreBase> History
		//	=> _history;

		//#region 現在の日付が変更された時の挙動
		//string _selectedDateText = DateTime.Today.ToShortDateString();
		//public string SelectedDateText {
		//	get { return _selectedDateText; }
		//	set {
		//		if (_selectedDateText == value) return;
		//		_selectedDateText = value;
		//		RaisePropertyChanged();
		//		AddNewRootCommand.RaiseCanExecuteChanged();
		//	}
		//}
		//ViewModelCommand addNewRootCommand;
		//public ViewModelCommand AddNewRootCommand {
		//	get {
		//		if (addNewRootCommand == null) {
		//			addNewRootCommand = new ViewModelCommand(() => {
		//				var r = ResultWithValue.Of<DateTime>(DateTime.TryParse, _selectedDateText);
		//				if (!r) return;
		//				var rt = RootCollection.GetOrCreate(r.Value);
		//				this.CurrentDate = rt.CurrentDate;
		//			}, () => ResultWithValue.Of<DateTime>(DateTime.TryParse, _selectedDateText).Result);
		//		}
		//		return addNewRootCommand;
		//	}
		//}


		//DateTreeRoot dtr = new DateTreeRoot();
		//public ObservableCollection<DateTree> DateList => dtr.Children;

		//#endregion

		//#region ロケーションツリーに対する操作
		//public async void ApplyCurrentPerPrice() {
		//	IsTreeLoading = true;
		//	var acs = this.Root.FirstOrDefault()?
		//		.Levelorder().Select(a => a.Model)
		//		.Where(a => a.GetNodeType() == IO.NodeType.Account)
		//		.OfType<AccountNode>();
		//	if (acs == null || !acs.Any() || this.CurrentDate == null) return;
		//	var lstLg = new List<string>();
		//	//var acse = acs.Select(a => new AccountEditVM(a));
		//	//foreach (var a in acse) {
		//	//	lstLg.AddRange(await a.ApplyPerPrice());
		//	//	a.Apply.Execute(null);
		//	//}
		//	IO.HistoryIO.SaveRoots((DateTime)this.CurrentDate);
		//	CommonNodeVM.ReCalcurate(this.Root.First());
		//	IsTreeLoading = false;
		//	if (lstLg.Any()) {
		//		string msg = "以下の銘柄は値を更新できませんでした。";
		//		var m = lstLg.Distinct().Aggregate(msg, (seed, ele) => seed + "\n" + ele);
		//		MessageBox.Show(m, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
		//	}
		//	else {
		//		MessageBox.Show("株価単価を更新しました。", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
		//	}
		//	//acse.ForEach(a => a.Dispose());
		//}
		//public void DeleteCurrentDate() {
		//	if (CurrentDate == null || RootCollection.Instance.IsEmpty()) return;
		//	Action delete = () => {
		//		var d = (DateTime)CurrentDate;
		//		RootCollection.Instance.Remove(this.Root.First().Model as TotalRiskFundNode);
		//		IO.HistoryIO.SaveRoots(d);
		//	};
		//	if (RootCollection.Instance.Last().CurrentDate == CurrentDate) {
		//		if (!this.Root.First().Model.HasTrading) {
		//			delete();
		//		}
		//		else {
		//			if (MessageBoxResult.OK == MessageBox.Show("取引情報が含まれています。削除しますか？", "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel)) {
		//				delete();
		//			}
		//		}
		//	}
		//	else if (!this.Root.First().Model.HasTrading) {
		//		if (MessageBoxResult.OK == MessageBox.Show(((DateTime)CurrentDate).ToString("yyyy年M月d日") + "の書込みデータを削除します。", "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.Cancel)) {
		//			delete();
		//		}
		//	}
		//	else {
		//		MessageBox.Show("取引情報が含まれている過去のデータは削除できません。", "Notice", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		//	}
		//}
		//public void ExpandCurrentNode() {
		//	if (!this.Path.Any()) return;
		//	var c = Root.SelectMany(a => a.Levelorder()).FirstOrDefault(a => a.Path.SequenceEqual(this.Path));
		//	if (c != null)
		//		foreach (var n in c.Upstream()) n.IsExpand = true;
		//}
		//public void ExpandAllNode() {
		//	if (Root.Any())
		//		foreach (var n in Root.SelectMany(a => a.Levelorder()))
		//			n.IsExpand = true;
		//}
		//public void CloseAllNode() {
		//	if (Root.Any())
		//		foreach (var n in Root.SelectMany(a => a.Levelorder()))
		//			n.IsExpand = false;
		//}
		//#endregion
		//#region 終了処理
		//HashSet<TotalRiskFundNode> _EditSet = new HashSet<TotalRiskFundNode>();
		//public void AddEditList(IEnumerable<CommonNode> nodes) {
		//	nodes.Select(a => a.Root())
		//		.OfType<TotalRiskFundNode>()
		//		.ForEach(a => _EditSet.Add(a));
		//}
		//public void AddEditList(CommonNode node) {
		//	this.AddEditList(new CommonNode[] { node });
		//}
		//public void EditEndAsExecute() {
		//	if (_EditSet.IsEmpty()) return;
		//	IO.HistoryIO.SaveRoots(_EditSet.Min(a => a.CurrentDate), _EditSet.Max(a => a.CurrentDate));
		//	_EditSet.Clear();
		//}
		//public void EditEndAsCancel() {
		//	if (_EditSet.IsEmpty()) return;
		//	RootCollection.Instance.Refresh();
		//	_EditSet.Clear();
		//}
		//#endregion
		//#region Controler as inner class
		///// <summary>ListViewModelを操作するためのインナークラス</summary>
		//private class VmControler : NotificationObject {
		//	EditViewModel lvm;
		//	public VmControler(EditViewModel vm) {
		//		lvm = vm;
		//	}

		//	public void RootCollectionChanged(object s, NotifyCollectionChangedEventArgs e) {
		//		lvm.dtr.Refresh();
		//		TotalRiskFundNode rt;
		//		switch (e.Action) {
		//		case NotifyCollectionChangedAction.Add:
		//			rt = e.NewItems.OfType<TotalRiskFundNode>().FirstOrDefault();
		//			if (rt == null) goto default;
		//			lvm.SetRoot(rt);
		//			break;
		//		default:
		//			var d = CurrentDate ?? DateTime.Today;
		//			rt = RootCollection.Instance.FirstOrDefault(a => d <= a.CurrentDate)
		//				?? RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= d);
		//			break;
		//		}
		//		if (rt == null) this.CurrentDate = null;
		//		else this.CurrentDate = rt.CurrentDate;
		//		lvm.RefreshHistory(this.Path);
		//	}

		//	DateTime? _currentDate;
		//	public DateTime? CurrentDate {
		//		get { return _currentDate; }
		//		set {
		//			if (_currentDate == value) return;
		//			if (value == null) {
		//				_currentDate = null;
		//				RaisePropertyChanged();
		//				lvm.SetRoot(null);
		//				return;
		//			}
		//			var r = RootCollection.Instance.FirstOrDefault(a => value <= a.CurrentDate)
		//				?? RootCollection.Instance.LastOrDefault(a => a.CurrentDate <= value);
		//			if (r == null) {
		//				_currentDate = null;
		//				RaisePropertyChanged();
		//				lvm.SetRoot(null);
		//				return;
		//			}
		//			_currentDate = r.CurrentDate;
		//			RaisePropertyChanged();
		//			lvm.SetRoot(r);
		//			lvm.dtr.SelectAt(r.CurrentDate);

		//			if (Path.Any()) {
		//				Path = r.SearchNodeOf(Path).Path;
		//			}
		//			else {
		//				Path = r.Path;
		//			}
		//		}
		//	}

		//	IEnumerable<string> _path = Enumerable.Empty<string>();
		//	public IEnumerable<string> Path {
		//		get { return _path; }
		//		set {
		//			value = value ?? Enumerable.Empty<string>();
		//			//if (_path.SequenceEqual(value)) return;
		//			var pt = value.ToArray();
		//			if (_path.SequenceEqual(pt)) return;
		//			_path = pt;
		//			RaisePropertyChanged();
		//			if (_path.Any()) {
		//				//lvm.ExpandCurrentNode();
		//				lvm.RefreshHistory(_path);
		//			}
		//		}
		//	}
		//}
		//#endregion
		#endregion
	}
	public class HistoryViewModel:ViewModel{
		public HistoryViewModel(){
			dpc = new DisposeCounter(() => IsHistoryLoading = true, () => IsHistoryLoading = false);
		}
		DisposeCounter dpc;
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
		bool _isOpen = false;
		public bool IsOpen{
			get => _isOpen;
			set{
				if (_isOpen == value) return;
				_isOpen = value;
				RaisePropertyChanged();
				if (_isOpen)
					EditPresenter.Instance.Messenger.Raise(new InteractionMessage("OpenHistoryFlyout"));
				else
					EditPresenter.Instance.Messenger.Raise(new InteractionMessage("CloseHistoryFlyout"));
			}
		}
		ViewModelCommand _CloseCmd;
		public ViewModelCommand CloseCmd{ get; }//書きかけ
		public void Refresh(IEnumerable<string> path,bool open = false){
			if(!IsOpen){
				if (open) IsOpen = true;
				else return;
			}
			using (dpc.Seald()){
				Collection = CommonNodeVM.ReCalcHistory(path);
			}
		}
		IEnumerable<VmCoreBase> _history = Enumerable.Empty<VmCoreBase>();
		public IEnumerable<VmCoreBase> Collection{
			get => _history;
			private set{
				_history = value;
				this.RaisePropertyChanged();
			}
		}
	}
	public class DisposeCounter {
		private class DelayFunc : IDisposable {
			public DelayFunc(Action end){_end = end; }
			Action _end;
			public void Dispose() {
				_end?.Invoke(); _end = null;
			}
		}
		Action _start;
		Action _end;
		public DisposeCounter(Action start,Action end){
			_start = start; _end = end;
		}
		public IDisposable Seald(){
			count++;
			if (count == 1) _start?.Invoke();
			return new DelayFunc(() => {
				count--;
				if (count == 0) _end?.Invoke();
			});
		}
		int count = 0;
	}
}
