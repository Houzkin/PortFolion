using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using System.Windows.Input;
using Houzkin.Architecture;
using PortFolion.Core;
using System.Collections.ObjectModel;
using Livet.Commands;
using Houzkin;
using System.ComponentModel;
using Livet.EventListeners.WeakEvents;
using System.Windows;
using PortFolion.IO;
using Livet;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Resources;

namespace PortFolion.ViewModels {
    /// <summary>
    /// 履歴表示時にVMとして振る舞うオブジェクト
    /// </summary>
	public class VmCoreBase : BindableObject {
		protected VmCoreBase() : base(new object()) { }
		public VmCoreBase(CommonNode node) : base(node) { }
		public VmCoreBase(CommonNode node, VmCoreGeneral cg) : base(node) {
			_invTtl = cg.InvestmentTotal;
			_invRTtl = cg.InvestmentReturnTotal;
		}
		public virtual void Copy(VmCoreGeneral core) {
			InvestmentTotal = core.InvestmentTotal;
			InvestmentReturnTotal = core.InvestmentReturnTotal;
			//AmountRate = core.AmountRate;
		}
		public DateTime? CurrentDate {
			get {
				return this.MaybeModelAs<CommonNode>().TrueOrNot(o => (o.Root() as TotalRiskFundNode)?.CurrentDate, x => null);
			}
		}
		#region common
		double _invTtl;
		public double InvestmentTotal {
			get { return _invTtl; }
			set {
				if (_invTtl == value) return;
				_invTtl = value;
				OnPropertyChanged();
			}
		}
		double _invRTtl;
		public double InvestmentReturnTotal {
			get { return _invRTtl; }
			set {
				if (_invRTtl == value) return;
				_invRTtl = value;
				OnPropertyChanged();
			}
		}
		#endregion

	}
    /// <summary>
    /// 履歴表示時にブローカーまたはアカウントのVM
    /// </summary>
	public class VmCoreBasket : VmCoreBase {
		protected VmCoreBasket() { }
		public VmCoreBasket(CommonNode node) : base(node) { }
		public VmCoreBasket(CommonNode node, VmCoreGeneral cg) : base(node, cg) {
			_pl = cg.ProfitLoss;
			_upl = cg.UnrealizedProfitLoss;
			_uplr = cg.UnrealizedPLRatio;
		}
		public override void Copy(VmCoreGeneral core) {
			base.Copy(core);
			ProfitLoss = core.ProfitLoss;
			UnrealizedProfitLoss = core.UnrealizedProfitLoss;
			UnrealizedPLRatio = core.UnrealizedPLRatio;
		}
		#region basket
		double _pl;
		public double ProfitLoss {
			get { return _pl; }
			set {
				if (_pl == value) return;
				_pl = value;
				OnPropertyChanged();
			}
		}
		double _upl;
		public double UnrealizedProfitLoss {
			get { return _upl; }
			set {
				if (_upl == value) return;
				_upl = value;
				OnPropertyChanged();
			}
		}
		double _uplr;
		public double UnrealizedPLRatio {
			get { return _uplr; }
			set {
				if (_uplr == value) return;
				_uplr = value;
				OnPropertyChanged();
			}
		}
		#endregion
	}
	public class VmCoreGeneral : VmCoreBasket  {
		public VmCoreGeneral() { }
		public VmCoreGeneral(CommonNode node) : base(node) { }
		public VmCoreGeneral(CommonNode node,VmCoreGeneral cg) : base(node, cg) {
			_pp = cg.PerPrice;
			_pbpa = cg.PerBuyPriceAverage;
		}
		public override void Copy(VmCoreGeneral core) {
			base.Copy(core);
			PerPrice = core.PerPrice;
			PerBuyPriceAverage = core.PerBuyPriceAverage;
		}
		#region product
		double _pp;
		public double PerPrice {
			get { return _pp; }
			set {
				if (_pp == value) return;
				_pp = value;
				OnPropertyChanged();
			}
		}
		double _pbpa;
		public double PerBuyPriceAverage {
			get { return _pbpa; }
			set {
				if (_pbpa == value) return;
				_pbpa = value;
				OnPropertyChanged();
			}
		}
		#endregion
	}
    /// <summary>
    /// ツリー表示用のVMとして振る舞う
    /// </summary>
	public class CommonNodeVM : ReadOnlyBindableTreeNode<CommonNode, CommonNodeVM> {
		#region static method
		public static CommonNodeVM Create(CommonNode node) {
			if (node == null) return null;
			var nt = node.GetNodeType();
			if(nt == NodeType.OtherProduct || nt == NodeType.Stock || nt == NodeType.Forex) {
				return new FinancialProductVM(node as FinancialProduct);
			}else if(nt == NodeType.Cash) {
				return new FinancialValueVM(node as FinancialValue);
			}else {
				return new FinancialBasketVM(node);
			}
		}
        /// <summary>
        /// 履歴を再計算し、結果を表示用インスタンスとして返す。
        /// </summary>
        /// <param name="path">再計算するパス</param>
        /// <returns>表示用インスタンス</returns>
		public static IEnumerable<VmCoreBase> ReCalcHistory(IEnumerable<string> path) {
			var ps = RootCollection.GetNodeLine(path).Values;
			var ps1 = ps.Select(a=>CommonNodeVM.Create(a));
			var p = new NodePath<string>(path);
			var dics = _com1(ps1);
			foreach (var dic in dics) {
				CommonNodeVM vm;
				if (dic.TryGetValue(p, out vm)) {
					yield return vm.ToHistoryVm();
				}
			}
		}
		public static void ReCalcurate(CommonNodeVM tgt) {
			DateTime date = (DateTime)tgt.CurrentDate;
			var dics = _com1(RootCollection.GetNodeLine(tgt.Root().Path, date)
				.TakeWhile(a => a.Key <= date)
				.Select(a => Create(a.Value)).ToArray());
			var dic = dics.LastOrDefault();
			if (dic == null) return;
			foreach (var ele in tgt.Levelorder().Reverse()) {
				CommonNodeVM vm;
				if (dic.TryGetValue(ele.Path, out vm)) {
					ele.CoreData.Copy(vm.CoreData);
				}
			}
			
		}
		static void _setTotal(bool r,CommonNodeVM pr,CommonNodeVM cu) {
			if (0 < cu.Model.InvestmentValue)
				cu.CoreData.InvestmentTotal = cu.Model.InvestmentValue;
			else if (cu.Model.InvestmentValue < 0)
				cu.CoreData.InvestmentReturnTotal = Math.Abs(cu.Model.InvestmentValue);
			if (r) {
				cu.CoreData.InvestmentTotal += pr.InvestmentTotal;
				cu.CoreData.InvestmentReturnTotal += pr.InvestmentReturnTotal;
			}
		}
		/// <summary>単価、含損益、平均取得額を設定する</summary>
		/// <param name="r">前回記入データの有無</param>
		/// <param name="pr">前回記入データ</param>
		/// <param name="cu">今回記入データ</param>
		static void _setPer(bool r,CommonNodeVM pr,CommonNodeVM cu) {
				if(cu.GetType() == typeof(FinancialBasketVM)) {
					cu.CoreData.UnrealizedProfitLoss = cu.Preorder()
						.OfType<FinancialProductVM>()
						.Sum(d => d.UnrealizedProfitLoss);
				}else if(cu.GetType() == typeof(FinancialProductVM)) {
					cu.MaybeModelAs<FinancialProduct>()
						.TrueOrNot(
						co => {
							cu.CoreData.PerPrice = co.Quantity == 0 ? 0 : co.Amount / co.Quantity;
							if (r) {
								var po = pr.Model as FinancialProduct;
								if(0 != po.Quantity) {
									//何倍に分割したか
									var tqRate = (co.Quantity - co.TradeQuantity)/po.Quantity;
									var preQt = po.Quantity * tqRate;
									var preAv = pr.CoreData.PerBuyPriceAverage / tqRate;

									if (0 <= co.InvestmentValue) {
										var preAm = preQt * preAv;
										cu.CoreData.PerBuyPriceAverage = (preAm + co.InvestmentValue) / (preQt + co.TradeQuantity);
									} else {
										cu.CoreData.PerBuyPriceAverage = preAv;
									}
								} else {
									cu.CoreData.PerBuyPriceAverage = 0;
								}
							}else {
								if(0 < co.InvestmentValue)
									cu.CoreData.PerBuyPriceAverage = co.Quantity == 0 ? 0 : co.InvestmentValue / co.Quantity;
							}
							cu.CoreData.UnrealizedProfitLoss = co.Amount - (cu.CoreData.PerBuyPriceAverage * co.Quantity);
						});
				}
		}
		static IEnumerable<Dictionary<NodePath<string>,CommonNodeVM>> _com1(IEnumerable<CommonNodeVM> nodes) {
			var nd = nodes
				.Select(a => a.Levelorder().Reverse().ToDictionary(b => b.Path, new keyselector()))
				.Scan(new Dictionary<NodePath<string>, CommonNodeVM>(), 
				(prv, cur) => {
					foreach(var c in cur) {
						var rst = ResultWithValue.Of<NodePath<string>, CommonNodeVM>(prv.TryGetValue, c.Key);
						_setTotal(rst.Result, rst.Value, c.Value);
						_setPer(rst.Result, rst.Value, c.Value);
					}
					return cur;
				});
			return nd;
		}
		static async Task<Dictionary<NodePath<string>,CommonNodeVM>[]> _com(IEnumerable<CommonNodeVM> nodes) {
			var t = await Task.Run(() => {
				return nodes
					.Select(a => a.Levelorder().Reverse().ToDictionary(b => b.Path, new keyselector()))
					.Scan(new Dictionary<NodePath<string>, CommonNodeVM>(),
					(prv, cur) => {
						foreach (var c in cur) {
							var rst = ResultWithValue.Of<NodePath<string>, CommonNodeVM>(prv.TryGetValue, c.Key);
							_setTotal(rst.Result, rst.Value, c.Value);
							_setPer(rst.Result, rst.Value, c.Value);
						}
						return cur;
					}).ToArray();
			});
			return t;
		}

		class keyselector : IEqualityComparer<NodePath<string>> {
			public bool Equals(NodePath<string> x, NodePath<string> y) {
				return x.SequenceEqual(y);
			}

			public int GetHashCode(NodePath<string> obj) {
				return obj.Aggregate("", (a, b) => a + b).GetHashCode();
			}
		}
		#endregion
		protected VmCoreGeneral CoreData { get; } = new VmCoreGeneral();
		IDisposable listenr;
		protected CommonNodeVM(CommonNode model) : base(model) {
			listenr = new PropertyChangedWeakEventListener(CoreData, corePropertyChanged);
			//CoreData.PropertyChanged += this.corePropertyChanged;
		}
		protected override void Dispose(bool disposing) {
			if (disposing) listenr.Dispose();
			base.Dispose(disposing);
		}
		protected override CommonNodeVM GenerateChild(CommonNode modelChildNode) {
			return Create(modelChildNode);
		}
		public virtual VmCoreBase ToHistoryVm() {
			return new VmCoreBase(this.Model, CoreData);
		}
		public void SetCoreData(VmCoreGeneral tgt) {
			this.CoreData.Copy(tgt);
			
		}
		private void corePropertyChanged(object sender, PropertyChangedEventArgs e) {
			this.OnPropertyChanged(e.PropertyName);
		}

		bool _isExpand = false;
		public bool IsExpand {
			get { return _isExpand; }
			set { this.SetProperty(ref _isExpand, value); }
		}
		public NodePath<string> Path => Model.Path;
		//public bool IsModelEquals(CommonNode node) => this.Model == node;
		public new CommonNode Model => base.Model;
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();

		public event Action<CommonNodeVM> ReCalcurated;
		private void RaiseReCalcurated(CommonNodeVM src) => ReCalcurated?.Invoke(src);
		public void ReCalcurate() {
			this.Root().RaiseReCalcurated(this);
		}
		public event Action<IEnumerable<string>> SetPath;
		private void RaiseSetPath(IEnumerable<string> path) => SetPath?.Invoke(path);
		public void DisplayHistory() {
			this.Root().RaiseSetPath(this.Path);
		}
		public DateTime? CurrentDate =>
			(Model.Root() as TotalRiskFundNode)?.CurrentDate;

		public double InvestmentTotal {
			get { return CoreData.InvestmentTotal; }
			set { CoreData.InvestmentTotal = value; }
		}
		public double InvestmentReturnTotal {
			get { return CoreData.InvestmentReturnTotal; }
			set { CoreData.InvestmentReturnTotal = value; }
		}
	}
    /// <summary>キャッシュポジション</summary>
	public class FinancialValueVM : CommonNodeVM {
		public FinancialValueVM(CommonNode model) : base(model) {
		}
	}
    /// <summary>非ポジション</summary>
	public class FinancialBasketVM : CommonNodeVM {
		public FinancialBasketVM(CommonNode model) : base(model) {
			var sp = new ViewModelCommand(() => DisplayHistory());
			
			MenuList.Add(new MenuItemVm(sp) { Header = "履歴を表示",
		});

			var ty = model.GetNodeType();
			if(ty == NodeType.Account) {
				var vc = new ViewModelCommand(() => {
					var vm = new AccountEditVM(model as AccountNode);
					var w = new Views.AccountEditWindow();
					w.DataContext = vm;
					var r = w.ShowDialog();
					if (vm.EdittingList.Any()) {
						//save or not
						if (r == true) {
							HistoryIO.SaveRoots(vm.EdittingList.Min(), vm.EdittingList.Max());
						} else {
							RootCollection.Instance.Refresh();
						}
						this.ReCalcurate();
					}
				});
				MenuList.Add(new MenuItemVm(vc) { Header = "編集" });
			}else if (ty == NodeType.Broker) {
				var vc = new ViewModelCommand(() => {
					var vm = new NodeNameEditerVM(model, new AccountNode(AccountClass.General));
					var w = new Views.NodeNameEditWindow();
					w.DataContext = vm;
					if(w.ShowDialog() == true && vm.EdittingList.Any()) {
						//save
						HistoryIO.SaveRoots(vm.EdittingList.Min(), vm.EdittingList.Max());
					}
				});
				MenuList.Add(new MenuItemVm(vc) { Header = "口座を追加" });
			}else if(ty == NodeType.Total) {
				var vc = new ViewModelCommand(() => {
					var vm = new NodeNameEditerVM(model, new BrokerNode());
					var w = new Views.NodeNameEditWindow();
					w.DataContext = vm;
					if(w.ShowDialog() == true && vm.EdittingList.Any()) {
						//save
						HistoryIO.SaveRoots(vm.EdittingList.Min(), vm.EdittingList.Max());
					}
				});
				MenuList.Add(new MenuItemVm(vc) { Header = "証券会社を追加" });
			}

			var vmc = new ViewModelCommand(() => {
				var vm = new NodeNameEditerVM(model.Parent, model);
				var w = new Views.NodeNameEditWindow();
				w.DataContext = vm;
				if(w.ShowDialog()==true && vm.EdittingList.Any()) {
					//save
					HistoryIO.SaveRoots(vm.EdittingList.Min(), vm.EdittingList.Max());
				}
			}, () => model.Parent != null);
			MenuList.Add(new MenuItemVm(vmc) { Header = "名前の変更" });

            var vmt = new ViewModelCommand(() => {
                var vm = new NodeTagEditerVM(model);
                var w = new Views.NodeTagEditWindow();
                w.DataContext = vm;
                if(w.ShowDialog()==true) { }
                //書きかけ
            });
            MenuList.Add(new MenuItemVm(vmt) { Header = "タグを変更" });
			
			if(ty == NodeType.Broker || ty == NodeType.Account) {
				var vc = new ViewModelCommand(() => {
					Action delete = () => {
						var d = this.Model.Upstream().OfType<TotalRiskFundNode>().LastOrDefault()?.CurrentDate;
						this.Model.Parent.RemoveChild(this.Model);
						HistoryIO.SaveRoots((DateTime)d);
					};
					if ((!this.Model.HasTrading && !this.Model.HasPosition)) {
						delete();
					} else {
						if (RootCollection.GetNodeLine(this.Model.Path).Values.Count == 1) {
							if (MessageBoxResult.OK == MessageBox.Show("ポジションまたは取引に関するデータを保持しています。削除しますか？", "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel)) {
								delete();
							}
						} else {
							MessageBox.Show("ポジションまたは取引に関するデータを保持しているため削除できません", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
						}
					}

				});
				MenuList.Add(new MenuItemVm(vc) { Header = "削除" });
			}
		}
		public override VmCoreBase ToHistoryVm() {
			return new VmCoreBasket(this.Model, CoreData);
		}
		public double ProfitLoss {
			get { return CoreData.ProfitLoss; }
			set { CoreData.ProfitLoss = value; }
		}
		public double UnrealizedProfitLoss {
			get { return CoreData.UnrealizedProfitLoss; }
			set { CoreData.UnrealizedProfitLoss = value; }
		}
		public double UnrealizedPLRatio {
			get { return CoreData.UnrealizedPLRatio; }
			set { CoreData.UnrealizedPLRatio = value; }
		}
	}
    /// <summary>金融商品</summary>
	public class FinancialProductVM : FinancialBasketVM {
		public FinancialProductVM(CommonNode model) : base(model) {
		}
		public override VmCoreBase ToHistoryVm() {
			return new VmCoreGeneral(this.Model, CoreData);
		}
		
		public double PerPrice {
			get { return CoreData.PerPrice; }
			set { CoreData.PerPrice = value; }
		}
		public double PerBuyPriceAverage {
			get { return CoreData.PerBuyPriceAverage; }
			set { CoreData.PerBuyPriceAverage = value; }
		}
	}
}
