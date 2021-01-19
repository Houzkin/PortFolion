using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using Livet.Commands;
using PortFolion.Core;
using PortFolion.IO;
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
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive;
using System.Reactive.Linq;

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
		/// <summary>ロケーションツリーのルートを示す。</summary>
		public ObservableCollection<CommonNodeVM> Root { get; } = new ObservableCollection<CommonNodeVM>();

		private IEnumerable<string> Path => History.Path;

		public HistoryViewModel History { get; } = new HistoryViewModel();
		private void setHistory(IEnumerable<string> path,bool open = false){
			History.Refresh(path, open);
			//RaisePropertyChanged(nameof(History));
		}
		public EditElementVm EditFlyoutVm { get; private set; }
		public void SetEditFlyoutVm(EditElementVm vm) {
			this.EditFlyoutVm?.CancelCmd.Execute();
			this.EditFlyoutVm = vm;
			this.RaisePropertyChanged(nameof(EditFlyoutVm));
			if (this.EditFlyoutVm != null) {
				this.EditFlyoutVm.Closed += _ => {
					EditViewModel.Instance.Messenger.Raise(new InteractionMessage("CloseEditFlyout"));
					EditFlyoutVm.Dispose();
					EditFlyoutVm = null;
				};
				EditViewModel.Instance.Messenger.Raise(new InteractionMessage("OpenEditFlyout"));
			}
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
				
				var p = lvm.Path.Any() ? r.SearchNodeOf(lvm.Path).Path : r.Path;
				if (lvm.History.SetPath(p)) lvm.History.Refresh(p, false);
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
				if(lvm.History.SetPath(path))
					this.RefreshHistory(path, true);
			}
			/// <summary>履歴データを更新する。</summary>
			/// <param name="path">履歴データのパス</param>
			/// <param name="open">openでなかった場合openするかどうか</param>
			public void RefreshHistory(IEnumerable<string> path,bool open = false){
				lvm.History.SetPath(path);
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
	}
	/// <summary>表示用TreeのVM部品</summary>
	public class TreeNodeProps : ViewModel {
		#region static
		public static TreeNodeProps CreateTreeVmComponent(CommonNodeVM vm){
			var tnv = new TreeNodeProps(vm);
			switch(vm.Model){
			case StockValue sv:
				break;
			case ForexValue fv:
				break;
			case FinancialProduct fp:
				break;
			case FinancialValue cash:
				break;
			case AnonymousNode an:
				break;
			case AccountNode ac:
				break;
			case TotalRiskFundNode tr:
				break;
			case BrokerNode bn:
				break;
			}
			switch(vm.Model.GetNodeType()){
			case NodeType.Stock: case NodeType.Forex:
				break;
			case NodeType.OtherProduct:
				break;
			case NodeType.Account:
				break;
			case NodeType.Broker:
				break;
			case NodeType.Total:
				break;
			}
			return tnv;
		}
		/// <summary>データグリッド上で編集するためのインスタンスを生成する</summary>
		static EditElementVm CreateEditElement(CommonNodeVM node){
			throw new NotImplementedException();
		}
		#region menu functions
		static MenuItemVm _editfunc(CommonNodeVM vm){
			return new MenuItemVm() { Header = "編集" };
		}
		static MenuItemVm _editTag(CommonNodeVM vm){
			return new MenuItemVm() { Header = "タグを変更" };
		}
		static MenuItemVm _moveAsset(CommonNodeVM vm){
			return new MenuItemVm() { Header = "資金を移動" };
		}
		static MenuItemVm _movePosition(CommonNodeVM vm){
			return new MenuItemVm() { Header = "ポジションを移動" };
		}
		static MenuItemVm _addAccount(CommonNodeVM vm){
			return new MenuItemVm() { Header = "口座を追加" };
		}
		static MenuItemVm _addBroker(CommonNodeVM vm){
			return new MenuItemVm() { Header = "証券会社を追加" };
		}
		static MenuItemVm _addPosition(CommonNodeVM vm){
			return new MenuItemVm() { Header = "ポジションを追加" };
		}
		static MenuItemVm _delete(CommonNodeVM vm){
			return new MenuItemVm() { Header = "削除" };
		}
		#endregion
		#endregion
		protected TreeNodeProps(CommonNodeVM vm){ ViewModel = vm; }
		private CommonNodeVM ViewModel{ get; }
		ObservableCollection<MenuItemVm> _menus;
		public ObservableCollection<MenuItemVm> MenuList => _menus = _menus ?? new ObservableCollection<MenuItemVm>();

		bool _IsEditMode = false;
		/// <summary>編集モードかどうかを示す値を返す</summary>
		public bool IsEditMode{
			get{ return _IsEditMode; }
			private set{
				if (_IsEditMode == value) return;
				_IsEditMode = value;
				this.RaisePropertyChanged();
				this.StartEditMode.RaiseCanExecuteChanged();
			}
		}
		EditElementVm _ElementVm;
		/// <summary>データグリッド上で編集するときに使用するプロパティ</summary>
		public EditElementVm ElementVm{
			get{ return _ElementVm; }
			private set{
				if (_ElementVm == value) return;
				_ElementVm = value;
				this.RaisePropertyChanged();
			}
		}
		ViewModelCommand _StartEditMode;
		/// <summary>データグリッド上で編集を開始するためのコマンド</summary>
		public ViewModelCommand StartEditMode => _StartEditMode = _StartEditMode
			?? new ViewModelCommand(() => {
				if (ElementVm == null) {
					ElementVm = CreateEditElement(ViewModel);
					ElementVm.Closed += _ => {
						this.IsEditMode = false;
						ElementVm = null;
					};
				}
				this.IsEditMode = true;
			}, () => !IsEditMode);
	}
	
	/// <summary>個別編集用のVM</summary>
	public abstract class EditElementVm : ViewModel {
		public EditElementVm(CommonNode model) {
			CurrentName = model.Name;
			Model = model;
			this.Reset = new ReactiveCommand();
			Reset.Subscribe(_ => this.ResetAction());
		}
		/// <summary>モデルノード名</summary>
		public string CurrentName{ get; }
		protected CommonNode Model{ get; private set; }
		public virtual string Title => "編集";
		
		void _execute(bool isApply){
			if(isApply){
				var nd = this.Execute();
				EditViewModel.Instance.AddEditList(nd);
			}
			Closed?.Invoke(isApply);
			//Closed = null;
		}
		public ReactiveProperty<string> Comment { get; } = new ReactiveProperty<string>();
		/// <summary>編集が終了したときに呼び出される</summary>
		public event Action<bool> Closed;
		ViewModelCommand _ExecuteCmd;
		public ViewModelCommand ExecuteCmd => _ExecuteCmd = _ExecuteCmd ?? new ViewModelCommand(
			() => _execute(true), CanExecute);
		ViewModelCommand _CancelCmd;
		public ViewModelCommand CancelCmd => _CancelCmd = _CancelCmd ?? new ViewModelCommand(
			() => _execute(false));
		protected abstract ISet<CommonNode> Execute();
		protected virtual bool CanExecute() => true;

		bool _isLoading = false;
		public bool IsLoading {
			get { return _isLoading; }
			set {
				if (_isLoading == value) return;
				_isLoading = value;
				RaisePropertyChanged();
				App.DoEvent();
			}
		}
		public ReactiveCommand Reset{ get; private set; }
		/// <summary>リセットをした時の挙動を設定</summary>
		protected virtual void ResetAction() { }
	}
	/// <summary>ノードを追加</summary>
	public class AddElementVm : EditElementVm{
		public AddElementVm(CommonNode parent,FinancialBasket child):this(parent,child as CommonNode){ }
		protected AddElementVm(CommonNode parent,CommonNode child) : base(parent){
			NewChild = child;
			Name = new ReactiveProperty<string>().SetValidateNotifyError(a => nameVali(a));
			Tag = new ReactiveProperty<string>(child.Tag.TagName);
			Name.Subscribe(a => this.ExecuteCmd.RaiseCanExecuteChanged());
		}
		public override string Title => $"{Model.Name}に要素を追加します";
		protected CommonNode NewChild{ get; }
		public ReactiveProperty<string> Name{ get; private set; }
		public ReactiveProperty<string> Tag{ get; private set; }

		IEnumerable<MenuItemVm> _tagCollection;
		public IEnumerable<MenuItemVm> TagCollection => _tagCollection = _tagCollection ?? ReadOnlyBindableCollection.Create(
			TagInfo.GetList(),
			ti => new MenuItemVm(() => this.Tag.Value = ti.TagName) { Header = ti.TagName });

		string nameVali(string name){
			var s = name.Trim();
			if (Model.Children.Any(a => a.Name == s)) return "名前の重複が存在します";
			return null;
		}
		protected override ISet<CommonNode> Execute() {
			NewChild.Name = Name.Value;
			NewChild.Tag = TagInfo.GetWithAdd(Tag.Value);
			this.Model.AddChild(NewChild);
			return new HashSet<CommonNode>() { Model.Root() };
		}
		protected override bool CanExecute() {
			var nm = Name.Value.Trim();
			return !string.IsNullOrEmpty(nm) && Model.Children.All(a => a.Name != nm);
		}
	}
	/// <summary>金融商品を追加</summary>
	public class AddProductElementVm : AddElementVm{
		public AddProductElementVm(CommonNode parent) : this(parent, new FinancialProduct()) { }
		protected AddProductElementVm(CommonNode parent,FinancialProduct child):base(parent,child){
			this.InvestmentValue = new ReactiveProperty<string>();
			this.DisplayInvestmentValue = this.InvestmentValue.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.TradeQuantity = new ReactiveProperty<string>();
			this.DisplayTradeQuantity = this.TradeQuantity.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.Amount = new ReactiveProperty<string>();
			this.DisplayAmount = this.Amount.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.PerPrice = new ReactiveProperty<string>();
			this.DisplayPerPrice = this.PerPrice.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();

			this.DisplayInvestmentValue.Subscribe(investV => {
				if (DisplayAmount.Value == 0) Amount.Value = InvestmentValue.Value;
				if (DisplayTradeQuantity.Value != 0 && investV != 0)
					PerPrice.Value = (DisplayInvestmentValue.Value / DisplayTradeQuantity.Value).ToString();
			});
			this.DisplayTradeQuantity.Subscribe(tq => {
				if (DisplayInvestmentValue.Value != 0 && tq != 0)
					PerPrice.Value = (DisplayInvestmentValue.Value / DisplayTradeQuantity.Value).ToString();
			});
			this.DisplayPerPrice.Subscribe(pp => {
				if (DisplayInvestmentValue.Value == 0 && DisplayTradeQuantity.Value != 0)
					InvestmentValue.Value = (pp * DisplayTradeQuantity.Value).ToString();
				else if (DisplayInvestmentValue.Value != 0 && DisplayTradeQuantity.Value == 0)
					TradeQuantity.Value = (DisplayInvestmentValue.Value / pp).ToString("0");
			});
		}
		public ReactiveProperty<string> InvestmentValue{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayInvestmentValue{ get; private set; }
		public ReactiveProperty<string> TradeQuantity{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayTradeQuantity{ get; private set; }
		public ReactiveProperty<string> Amount{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayAmount{ get; private set; }
		public ReactiveProperty<string> PerPrice{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayPerPrice{ get; private set; }
		public override string Title => $"{Model.Name}にポジションを追加します";
		protected new FinancialProduct NewChild => base.NewChild as FinancialProduct;
		protected override ISet<CommonNode> Execute() {
			NewChild.SetAmount((long)this.DisplayAmount.Value);
			NewChild.SetInvestmentValue((long)this.DisplayInvestmentValue.Value);
			NewChild.SetTradeQuantity((long)this.DisplayTradeQuantity.Value);
			NewChild.SetQuantity(NewChild.TradeQuantity);
			return base.Execute();
		}
	}
	/// <summary>Stockを追加</summary>
	public class AddStockElementVm : AddProductElementVm{
		public AddStockElementVm(CommonNode parent) : base(parent,new StockValue()){
			Code = new ReactiveProperty<string>();
			SetPerPrice = Code.Where(a => a.Count() == 4)
				.Select(a => ResultWithValue.Of<int>(int.TryParse, a).Result).ToReactiveCommand();
			SetPerPrice.Subscribe(a => {
				//var tt = Web.TickerTable.Create((Model.Root() as TotalRiskFundNode).CurrentDate).FirstOrDefault(b => b.Symbol == Code.Value);
				var tt = Web.Kabuoji3.GetInfo((Model.Root() as TotalRiskFundNode).CurrentDate, Code.Value);
				if(tt != null){
					this.PerPrice.Value = tt.Close.ToString("#,#.#");
					this.Comment.Value = $"{Code.Value}の終値を適用しました";
				}else{
					this.PerPrice.Value = "0";
					this.Comment.Value = $"{Code.Value}は見つかりませんでした"; 
				}
			});
		}
		private new StockValue NewChild => base.NewChild as StockValue;
		public ReactiveProperty<string> Code{ get; private set; }
		public ReactiveCommand SetPerPrice { get; private set; }
		protected override bool CanExecute() {
			if (Code.Value.Count() != 4) return false;
			return ResultWithValue.Of<int>(int.TryParse, Code.Value)
				.TrueOrNot(o => base.CanExecute(), x => false);
		}
		protected override ISet<CommonNode> Execute() {
			NewChild.Code = ResultWithValue.Of<int>(int.TryParse, Code.Value).Value;
			return base.Execute();
		}
	}

	/// <summary>資金移動用VMのベースクラス</summary>
	public abstract class AssetMoveEditElementVm : EditElementVm{
		public class MoveMenuItem : MenuItemVm{
			public MoveMenuItem(CommonNode node, Action<CommonNode> action,IEnumerable<CommonNode> display){
				this.MenuCommand = new ViewModelCommand(() => action(node), () => !display.Contains(node));
				this.Header = node.Name;
				node.Children
					.Select(
						x => x.Convert(a => 
							new MenuItemVm(
								() => action?.Invoke(a), 
								() => !display.Contains(a)) { Header = a.Name },
							(a, b) => a.Children.Add(b)))
					.ForEach(a => this.Children.Add(a));
			}
		}
		protected AssetMoveEditElementVm(FinancialValue leaf) : base(leaf) {
			_SelectedNode = new ReactiveProperty<CommonNode>();
			_TargetNode = _SelectedNode.Select(a => SelectedToTarget(a)).ToReadOnlyReactiveProperty();

			_SelectedNode.Subscribe(a => {
				this.RaisePropertyChanged(nameof(DisplaySelectedPath));
				this.MoveTo.Clear();
				a.Upstream().Reverse().ForEach(b => this.MoveTo.Add(new MoveMenuItem(b, cn => _SelectedNode.Value = cn, a.Upstream())));
			});
			_TargetNode.Subscribe(_ =>{
				this.ExecuteCmd.RaiseCanExecuteChanged();
				RaisePropertyChanged(nameof(DisplayTargetParentPath));
			});
			
			_SelectedNode.Value = leaf;
		}
		protected virtual CommonNode SelectedToTarget(CommonNode node) => node;
		protected ReactiveProperty<CommonNode> _SelectedNode { get; private set; } 
		protected ReadOnlyReactiveProperty<CommonNode> _TargetNode{ get; private set; }
		public ObservableCollection<MenuItemVm> MoveTo { get; } = new ObservableCollection<MenuItemVm>();
		public string DisplayCurrentPath => Model.Path.Aggregate("", (a, b) => a + "/" + b);
		public string DisplaySelectedPath => MoveTo.Aggregate("", (a, b) => a + "/" + b.Header);//_SelectedNode.Path.Aggregate("", (a, b) => a + "/" + b);
		public string DisplayTargetParentPath{
			get{
				if (_TargetNode.Value == null) return "";
				if (_TargetNode.Value.Root() == this.Model.Root())
					return Model.Parent.Path.Aggregate("", (a, b) => a + "/" + b);
				return DisplaySelectedPath;
			}
		}
		protected override ISet<CommonNode> Execute() {
			return new HashSet<CommonNode>() { Model.Root() };
		}
		protected override void ResetAction() => _SelectedNode.Value = this.Model; //selectedAction(this.Model);
	}
	/// <summary>CashPositionの資金移動</summary>
	public class CashMoveEditElementVm : AssetMoveEditElementVm{
		public CashMoveEditElementVm(FinancialValue cash) : base(cash){
			if (cash.GetNodeType() != NodeType.Cash) throw new ArgumentException("CashNode only!");

			MoveAmount = new ReactiveProperty<string>("0");
			CurrentAmount = new ReactiveProperty<string>(Model.Amount.ToString());

			DisplayMoveAmount = MoveAmount.Select(a => Math.Abs(ExpParse.Try(a))).ToReadOnlyReactiveProperty();
			DisplayCurrentAmount = CurrentAmount.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();

			DisplayMoveAmount.Subscribe(a => {
				if (0 < a) CurrentAmount.Value = $"{Model.Amount.ToString()} - {a}"; 
				//else if (a < 0) CurrentAmount.Value = $"{Model.Amount.ToString()} - ({a})";
			});

		}
		public override string Title => "資金移動";
		protected override CommonNode SelectedToTarget(CommonNode node) {
			var t = node.Preorder().Where(a => a.GetNodeType() == NodeType.Cash && a != this.Model);
			if (t.Count() == 1) return t.First();
			return null;
		}
		private new FinancialValue Model => this.Model as FinancialValue;
		private FinancialValue Target => this._TargetNode.Value as FinancialValue;

		public ReactiveProperty<string> CurrentAmount{ get; private set; }  
		public ReadOnlyReactiveProperty<double> DisplayCurrentAmount{ get; private set; }
		public ReactiveProperty<string> MoveAmount{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayMoveAmount{ get; private set; }

		protected override void ResetAction() {
			MoveAmount.Value = "0";
			CurrentAmount.Value = Model.Amount.ToString();
			base.ResetAction();
		}
		protected override ISet<CommonNode> Execute() {
			Model.SetAmount((long)DisplayCurrentAmount.Value);
			Model.SetInvestmentValue((long)(-DisplayMoveAmount.Value));
			Target.SetAmount((long)(Target.Amount + DisplayMoveAmount.Value));
			Target.SetInvestmentValue((long)(DisplayMoveAmount.Value));
			return base.Execute();
		}
		protected override bool CanExecute() {
			return this._TargetNode.Value != null || DisplayMoveAmount.Value == 0;
		}
	}
	/// <summary>Positionの移動</summary>
	public class PositionMoveEditElementVm : AssetMoveEditElementVm{
		public PositionMoveEditElementVm(FinancialProduct product) : base(product){
			this.CurrentQuantity = new ReactiveProperty<string>(this.Model.Quantity.ToString());
			this.DisplayCurrentQuantity = this.CurrentQuantity.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.CurrentAmount = new ReactiveProperty<string>(this.Model.Amount.ToString());
			this.DisplayCurrentAmount = this.CurrentAmount.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.MoveQuantity = new ReactiveProperty<string>();
			this.DisplayMoveQuantity = this.MoveQuantity.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.TargetQuantity = new ReactiveProperty<string>();
			this.DisplayTargetQuantity = this.TargetQuantity.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.TargetAmount = new ReactiveProperty<string>();
			this.DisplayTargetAmount = this.TargetAmount.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			
			void action() {
				if (this._TargetNode.Value == null){
					TargetAmount.Value = "0";
					TargetQuantity.Value = "0";
					return;
				}
				double a = DisplayMoveQuantity.Value;
				var per = this.Model.Amount / this.Model.Quantity;
				CurrentAmount.Value = Model.Amount.ToString() + $"{(a != 0 ? $" - {per} * {a}" : "")}";
				CurrentQuantity.Value = Model.Quantity.ToString() + $"{(a != 0 ? $" - {a}" : "")}";
				TargetAmount.Value = TargetAmount.ToString() + $"{(a != 0 ? $" + {per} * {a}" : "")}";
				TargetQuantity.Value = TargetQuantity.ToString() + $"{(a != 0 ? $" + {a}" : "")}";
			}
			DisplayMoveQuantity.Subscribe(_ => action());
			this._TargetNode.Subscribe(_ => action());
		}
		#region reactives
		/// <summary>現在の数量</summary>
		public ReactiveProperty<string> CurrentQuantity{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayCurrentQuantity{ get; private set; }
		/// <summary>移動数量</summary>
		public ReactiveProperty<string> MoveQuantity{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayMoveQuantity{ get; private set; }
		/// <summary>現在の残高</summary>
		public ReactiveProperty<string> CurrentAmount{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayCurrentAmount{ get; private set; }
		/// <summary>対象の数量</summary>
		public ReactiveProperty<string> TargetQuantity{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayTargetQuantity{ get; private set; }
		/// <summary>対象の残高</summary>
		public ReactiveProperty<string> TargetAmount{ get; private set; }
		public ReadOnlyReactiveProperty<double> DisplayTargetAmount{ get; private set; }
		#endregion
		public override string Title => "ポジション移動";
		private new FinancialProduct Model => this.Model as FinancialProduct;
		private FinancialProduct Target => this._TargetNode.Value as FinancialProduct;
		protected override CommonNode SelectedToTarget(CommonNode node) {
			IEnumerable<FinancialProduct> func() {
				var t = node.Preorder().Where(a => a.GetNodeType() == this.Model.GetNodeType() && a != this.Model);
				switch (this.Model.GetNodeType()) {
				case NodeType.Stock:
					return t.OfType<StockValue>().Where(a => a.Code == (this.Model as StockValue).Code);
				case NodeType.OtherProduct:
					return t.OfType<FinancialProduct>().Where(a => a.Name == this.Model.Name);
				default:
					throw new ArgumentException();
				}
			}
			FinancialProduct func1(){
				var rst = func();
				if (rst.Count() == 1) return rst.First();
				if (rst.Any()) return null;
				var cln = Model.Clone() as FinancialProduct;
				cln.SetAmount(0);
				cln.SetQuantity(0);
				return cln;
			}
			var n = func1();
			if (n == null) this.Comment.Value = "移動先を特定できませんでした";
			else Comment.Value = "";
			return n;
		}
		protected override bool CanExecute() 
			=> this._TargetNode.Value != null || this.DisplayMoveQuantity.Value == 0;
		protected override ISet<CommonNode> Execute() {
			if (this.Model.Root() != this.Target.Root()) {
				this._SelectedNode.Value.AddChild(this.Target);
			}
			this.Model.SetAmount((long)this.DisplayCurrentAmount.Value);
			this.Model.SetQuantity((long)this.DisplayCurrentQuantity.Value);
			this.Target.SetAmount((long)this.DisplayTargetAmount.Value);
			this.Target.SetQuantity((long)this.DisplayTargetQuantity.Value);
			return base.Execute();
		}
		protected override void ResetAction() {
			this.MoveQuantity.Value = "0";
			base.ResetAction();
		}
	}
	/// <summary>ノードの名前またはタグを変更する場合のVM</summary>
	public class NodeEditElementVm : EditElementVm{
		public NodeEditElementVm(CommonNode model):base(model){
			this.Name = new ReactiveProperty<string>(Model.Name)
				.SetValidateNotifyError(a => vali(a.Trim()))
				.AddTo(this.CompositeDisposable);
			this.Name.Subscribe(a => this.ExecuteCmd.RaiseCanExecuteChanged());
			this.NameEditParam = new ReactiveProperty<Core.TagEditParam>(Core.TagEditParam.Position)
				.AddTo(this.CompositeDisposable);
			this.NameEditParam.Subscribe(a => {
				this.Name.ForceValidate();
				this.ExecuteCmd.RaiseCanExecuteChanged();
			});

			this.CurrentTag = Model.Tag.TagName;
			this.Tag = new ReactiveProperty<string>(Model.Tag.TagName)
				.AddTo(this.CompositeDisposable);
			this.TagEditParam = new ReactiveProperty<Core.TagEditParam>(Core.TagEditParam.Position)
				.AddTo(this.CompositeDisposable);
		}
		public ReactiveProperty<string> Name{ get; }
		public ReactiveProperty<TagEditParam> NameEditParam{ get; }
		Task<string> vali(string name){
			return new Task<string>(() => {
				if (CurrentName == name){
					Comment.Value = "";
					return null;
				}
				var r = RootCollection.CanChangeNodeName(Model, Name.Value, NameEditParam.Value);
				if (!r.Result) {
					Comment.Value = "";
					if (1 == r.Value.Count())
						return $"[{name}]は{r.Value.First().Key:yyyy-M-d}において重複が存在します。";
					else
						return $"[{name}]は{r.Value.First().Key:yyyy-M-d}から{r.Value.Last().Key:yyyy-M-d}の期間で重複が存在します。";
				}
				var p = Model.Parent.Path.Concat(new string[] { name });
				if (RootCollection._GetNodeLine(p).Any())
					Comment.Value = $"[{name}]は別の時系列に存在します。\n変更後、既存の[{name}]と同一のものとして扱われます。";
				else Comment.Value = "";
				return null;
			});
		}
		IEnumerable<MenuItemVm> _tagCollection;
		public IEnumerable<MenuItemVm> TagCollection => _tagCollection = _tagCollection ?? ReadOnlyBindableCollection.Create(
			TagInfo.GetList(),
			ti => new MenuItemVm(() => this.Tag.Value = ti.TagName) { Header = ti.TagName });
		public string CurrentTag{ get; }
		public ReactiveProperty<string> Tag{ get; }
		public ReactiveProperty<TagEditParam> TagEditParam{ get; }

		protected override ISet<CommonNode> Execute() {
			var lst = new HashSet<CommonNode>();
			var nm = this.Name.Value.Trim();
			if(CurrentName != nm){
				RootCollection.ChangeNodeName(this.Model, nm, this.NameEditParam.Value)
				   .ForEach(a => lst.Add(a));
			}
			var ntg = this.Tag.Value.Trim();
			if(CurrentTag != ntg){
				RootCollection.ChangeTag(this.Model, ntg, this.TagEditParam.Value)
					.ForEach(a => lst.Add(a));
			}
			return lst;
		}
		protected override void ResetAction() {
			this.Name.Value = CurrentName;
			this.Tag.Value = CurrentTag;
		}
	}
	/// <summary>各項目を編集するVM</summary>
	public class CashEditElementVm : EditElementVm{
		public CashEditElementVm(FinancialValue model) : base(model) {
			this.InvestmentValue = new ReactiveProperty<string>(Model.InvestmentValue.ToString());
			this.DisplayInvestmentValue = InvestmentValue
				.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();
			this.Amount = new ReactiveProperty<string>(Model.Amount.ToString());
			this.DisplayAmount = Amount
				.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();

			//this.Closed += a => this.IsEditMode = false;
		}

		protected new FinancialValue Model => base.Model as FinancialValue;
		public ReactiveProperty<string> InvestmentValue{ get; }
		public ReadOnlyReactiveProperty<double> DisplayInvestmentValue{ get; protected set; }
		public ReactiveProperty<string> Amount{ get; }
		public ReadOnlyReactiveProperty<double> DisplayAmount{ get; }
		protected override ISet<CommonNode> Execute() {
			var set = new HashSet<CommonNode>();
			var rslt = new List<bool>(){
				Model.SetAmount((long)DisplayAmount.Value),
				Model.SetInvestmentValue((long)DisplayInvestmentValue.Value),
			};
			if(rslt.Any(_=>_)) set.Add(Model);
			return set;
		}
		protected override void ResetAction() {
			this.InvestmentValue.Value = Model.InvestmentValue.ToString();
			this.Amount.Value = Model.Amount.ToString();
			base.ResetAction();
		}
	}
	public class ProductEditElementVm : CashEditElementVm{
		public ProductEditElementVm(FinancialProduct model) : base(model){
			PerPrice = new ReactiveProperty<string>(Model.Quantity != 0 ? (Model.Amount / Model.Quantity).ToString("#.##") : "0");
			DisplayPerPrice = PerPrice.Select(a=>ExpParse.Try(a)).ToReadOnlyReactiveProperty();

			TradeQuantity = new ReactiveProperty<string>(Model.TradeQuantity.ToString());
			DisplayTradeQuantity = TradeQuantity.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();

			Quantity = new ReactiveProperty<string>(Model.TradeQuantity.ToString());
			DisplayQuantity = Quantity.Select(a => ExpParse.Try(a)).ToReadOnlyReactiveProperty();

			DisplayPerPrice
				.Subscribe(a=> this.Amount.Value = (DisplayQuantity.Value * DisplayPerPrice.Value).ToString());
			DisplayTradeQuantity
				.Subscribe(a => Quantity.Value = (Model.Quantity + a).ToString());
			DisplayQuantity
				.Subscribe(a => this.Amount.Value = (a * DisplayPerPrice.Value).ToString());
			DisplayInvestmentValue = DisplayInvestmentValue
				.Select(a => DisplayTradeQuantity.Value < 0 ? Math.Abs(a) * -1
					: 0 < DisplayTradeQuantity.Value ? Math.Abs(a) : a)
				.ToReadOnlyReactiveProperty();
		}
		protected new FinancialProduct Model => base.Model as FinancialProduct;
		public ReactiveProperty<string> PerPrice{ get; }
		public ReadOnlyReactiveProperty<double> DisplayPerPrice{ get; }

		public ReactiveProperty<string> TradeQuantity{ get; }
		public ReadOnlyReactiveProperty<double> DisplayTradeQuantity{ get; }

		public ReactiveProperty<string> Quantity{ get; }
		public ReadOnlyReactiveProperty<double> DisplayQuantity{ get; }
		protected override ISet<CommonNode> Execute() {
			var bs = base.Execute();
			var rslt = new List<bool>(){
				Model.SetQuantity((long)this.DisplayQuantity.Value),
				Model.SetTradeQuantity((long)this.DisplayTradeQuantity.Value)
			};
			if (rslt.Any(_ => _)) bs.Add(Model);
			return bs;
		}
		protected override void ResetAction() {
			this.PerPrice.Value = Model.Quantity != 0 ? (Model.Amount / Model.Quantity).ToString("#.##") : "0";
			this.TradeQuantity.Value = Model.TradeQuantity.ToString();
			this.Quantity.Value = Model.TradeQuantity.ToString();
			base.ResetAction();
		}
	}
	public class StockEditElementVm : ProductEditElementVm {
		public StockEditElementVm(StockValue model) : base(model) {
			this.Code = new ReactiveProperty<string>(Model.Code.ToString())
				.SetValidateNotifyError(a=>_codeVali(a));
			this.Code.Subscribe(_ => ApplySymbol.RaiseCanExecuteChanged());
		}
		protected new StockValue Model => base.Model as StockValue;
		public ReactiveProperty<string> Code{ get; }
		string _codeVali(string value){
			return ResultWithValue.Of<int>(int.TryParse, value)
				.TrueOrNot(
					o => value.Count() == 4 ? null : "4桁",
					x => "コードを入力してください");
		}
		ViewModelCommand _ApplySymbol;
		public ViewModelCommand ApplySymbol
			=> _ApplySymbol = _ApplySymbol ?? new ViewModelCommand(
				_applySymbol,
				() => _codeVali(this.Code.Value) == null);
		void _applySymbol(){
			this.IsLoading = true;
			var d = (Model.Root() as TotalRiskFundNode).CurrentDate;
			//var tt = Web.TickerTable.Create(d);
			//this.PerPrice.Value = tt.FirstOrDefault(a => a.Symbol == this.Code.Value)?.Close.ToString();
			this.PerPrice.Value = Web.Kabuoji3.GetInfo(d, this.Code.Value).Close.ToString();
			this.IsLoading = false;
		}
		protected override ISet<CommonNode> Execute() {
			var bs = base.Execute();
			ResultWithValue.Of<int>(int.TryParse, this.Code.Value)
				.TrueOrNot(a => {
					Model.Code = a;
					bs.Add(Model);
				});
			return bs;
		}
		protected override void ResetAction() {
			this.Code.Value = Model.Code.ToString();
			base.ResetAction();
		}
	}

	public class HistoryViewModel:ViewModel{
		public HistoryViewModel(){
			dpc = new DisposableBlock(() => IsHistoryLoading = true, () => IsHistoryLoading = false);
		}
		DisposableBlock dpc;
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
					EditViewModel.Instance.Messenger.Raise(new InteractionMessage("OpenHistoryFlyout"));
				else
					EditViewModel.Instance.Messenger.Raise(new InteractionMessage("CloseHistoryFlyout"));
			}
		}
		ViewModelCommand _CloseCmd;
		public ViewModelCommand ClosedCmd => 
			_CloseCmd = _CloseCmd ?? new ViewModelCommand(() => this.IsOpen = false);

		public void Refresh(IEnumerable<string> path,bool open = false){
			if(!IsOpen){
				if (open) IsOpen = true;
				else return;
			}
			using (dpc.Block()){
				Collection = CommonNodeVM.ReCalcHistory(path);
			}
		}
		public IEnumerable<string> Path { get; private set; } = Enumerable.Empty<string>();
		public bool SetPath(IEnumerable<string> path){
			if (Path.SequenceEqual(path)) return false;
			Path = path;
			RaisePropertyChanged(nameof(Path));
			return true;
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
	public class DisposableBlock {
		private class DispAction : IDisposable {
			public DispAction(Action end){_end = end; }
			Action _end;
			public void Dispose() {
				_end?.Invoke(); _end = null;
			}
		}
		Action _start;
		Action _end;
		public DisposableBlock(Action start,Action end){
			_start = start; _end = end;
		}
		public IDisposable Block(){
			count++;
			if (count == 1) _start?.Invoke();
			return new DispAction(() => {
				count--;
				if (count == 0) _end?.Invoke();
			});
		}
		int count = 0;
	}
}
