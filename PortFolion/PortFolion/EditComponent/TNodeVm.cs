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

namespace PortFolion.ViewModels {
	public class VmCoreBase : NotificationObject {
		double _invTtl;
		public double InvestmentTotal {
			get { return _invTtl; }
			set {
				if (_invTtl == value) return;
				_invTtl = value;
				RaisePropertyChanged();
			}
		}
		double _invRTtl;
		public double InvestmentReturnTotal {
			get { return _invRTtl; }
			set {
				if (_invRTtl == value) return;
				_invRTtl = value;
				RaisePropertyChanged();
			}
		}
		double _ar;
		public double AmountRate {
			get { return _ar; }
			set {
				if (_ar == value) return;
				_ar = value;
				RaisePropertyChanged();
			}
		}
	}
	public class VmCoreBasket : VmCoreBase {

		double _pl;
		public double ProfitLoss {
			get { return _pl; }
			set {
				if (_pl == value) return;
				_pl = value;
				RaisePropertyChanged();
			}
		}
		double _upl;
		public double UnrealizedProfitLoss {
			get { return _upl; }
			set {
				if (_upl == value) return;
				_upl = value;
				RaisePropertyChanged();
			}
		}
		double _uplr;
		public double UnrealizedPLRatio {
			get { return _uplr; }
			set {
				if (_uplr == value) return;
				_uplr = value;
				RaisePropertyChanged();
			}
		}
	}
	public class VmCoreGeneral : VmCoreBasket {
		double _pp;
		public double PerPrice {
			get { return _pp; }
			set {
				if (_pp == value) return;
				_pp = value;
				RaisePropertyChanged();
			}
		}
		double _pbpa;


		public double PerBuyPriceAverage {
			get { return _pbpa; }
			set {
				if (_pbpa == value) return;
				_pbpa = value;
				RaisePropertyChanged();
			}
		}
	}

	public class CommonVm : ReadOnlyBindableTreeNode<CommonNode, CommonVm> {
		#region static method
		public static CommonVm Create(CommonNode node) {
			if (node == null) return null;
			var nt = node.GetNodeType();
			if(nt == NodeType.OtherProduct || nt == NodeType.Stock || nt == NodeType.Forex) {
				return new ProductVm(node as FinancialProduct);
			}else if(nt == NodeType.Cash) {
				return new FinancialVm(node as FinancialValue);
			}else {
				return new BasketVm(node);
			}
		}
		private class Scanable {
			public VmCoreGeneral Current { get; set; } = new VmCoreGeneral();
			public IEnumerable<VmCoreGeneral> Products { get; set; }
		}
		
		public static IEnumerable<CommonVm> ReCalcHistory(IEnumerable<string> path) {
			var nd = RootCollection.GetNodeLine(path).Values
				.Select(a => Create(a))
				.Scan(new Scanable(), (pre, cur) => {
					var sbl = new Scanable();
					sbl.Current.InvestmentTotal = pre.Current.InvestmentTotal + cur.InvestmentTotal;
					sbl.Current.InvestmentReturnTotal = pre.Current.InvestmentReturnTotal + cur.InvestmentReturnTotal;
					if (!cur.Model.IsRoot()) { sbl.Current.AmountRate = cur.Model.Amount / cur.Model.Parent.Amount * 100; }
					//
					return sbl;
				});
			throw new NotImplementedException();
		}
		public static void ReCalcurate(CommonVm src) {
			var refList = src.Levelorder().Skip(1).Reverse()
				.Concat(src.Siblings())
				.Concat(src.Upstream().Skip(1));
			foreach(var nd in refList) {
				var d = (DateTime)nd.CurrentDate;
				var cpl = RootCollection.GetNodeLine(nd.Path, d)
					.Where(a => a.Key <= d)
					.ToDictionary(a => a.Key, a => a.Value);
				calcCommon(cpl, nd);
				if (nd.GetType() == typeof(BasketVm)) calcBasket(nd as BasketVm);
				else if (nd.GetType () == typeof(ProductVm)) calcProduct(cpl, nd as ProductVm);
				if((nd as BasketVm) != null)
					nd.CoreData.UnrealizedPLRatio = nd.Model.Amount != 0 ? nd.CoreData.UnrealizedProfitLoss / nd.Model.Amount * 100 : 0;
			}
		}
		static void calcCommon(Dictionary<DateTime,CommonNode> dic, CommonVm vm) {
			vm.CoreData.InvestmentTotal = dic.Where(a => 0 < a.Value.InvestmentValue).Sum(a => a.Value.InvestmentValue);
			vm.CoreData.InvestmentReturnTotal = Math.Abs(dic.Where(a => 0 > a.Value.InvestmentValue).Sum(a => a.Value.InvestmentValue));
			if (!vm.Model.IsRoot()) {
				vm.CoreData.AmountRate = vm.Model.Amount / vm.Model.Parent.Amount * 100;
			}
		}
		static void calcBasket(BasketVm vm) {
			vm.CoreData.ProfitLoss = vm.Model.Amount - vm.CoreData.InvestmentTotal - vm.CoreData.InvestmentReturnTotal;
			vm.CoreData.UnrealizedProfitLoss = vm.Preorder().OfType<ProductVm>().Sum(a => a.UnrealizedProfitLoss);//vm.Children.OfType<BasketVm>().Sum(a => a.UnrealizedProfitLoss);
		}
		static void calcProduct(Dictionary<DateTime,CommonNode> dic, ProductVm vm) {
			vm.MaybeModelAs<FinancialProduct>().TrueOrNot(
				o => vm.CoreData.PerPrice = o.Amount / o.Quantity);
			var m = dic.Select(a => a.Value).OfType<FinancialProduct>();
			if (m.Any()) {
				var nml = m.Zip(m.Skip(1), (a, b) => a.Quantity == 0 ? 1d : (b.Quantity - b.TradeQuantity) / a.Quantity)
					.Concat(new double[] { 1d })
					.Reverse()
					.Scan(1d, (a, b) => a * b)
					.Reverse()
					.Zip(m, (r, fp) => new { TQuantity = fp.TradeQuantity * r, TAmount = fp.InvestmentValue })
					.Where(a => a.TQuantity > 0)
					.Aggregate(
						new { TQuantity = 1d, TAmount = 1d },
						(a, b) => new { TQuantity = a.TQuantity + b.TQuantity, TAmount = a.TAmount + b.TAmount });
				vm.CoreData.PerBuyPriceAverage = nml.TAmount / nml.TQuantity;
			}else {
				vm.CoreData.PerBuyPriceAverage = 0;
			}
			vm.MaybeModelAs<FinancialProduct>().TrueOrNot(
				o => vm.CoreData.UnrealizedProfitLoss = o.Amount - (vm.PerBuyPriceAverage * o.Quantity));

		}
		#endregion
		protected VmCoreGeneral CoreData { get; } = new VmCoreGeneral();
		protected CommonVm(CommonNode model) : base(model) {
			CoreData.PropertyChanged += this.corePropertyChanged;
		}
		protected override CommonVm GenerateChild(CommonNode modelChildNode) {
			return Create(modelChildNode);
		}
		public void SetCore(VmCoreGeneral core) {
			CoreData.InvestmentTotal = core.InvestmentTotal;
			CoreData.InvestmentReturnTotal = core.InvestmentReturnTotal;
			CoreData.AmountRate = core.AmountRate;
			CoreData.ProfitLoss = core.ProfitLoss;
			CoreData.UnrealizedProfitLoss = core.UnrealizedProfitLoss;
			CoreData.UnrealizedPLRatio = core.UnrealizedPLRatio;
			CoreData.PerPrice = core.PerPrice;
			CoreData.PerBuyPriceAverage = core.PerBuyPriceAverage;
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
		public bool IsModelEquals(CommonNode node) => this.Model == node;
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();

		public event Action<CommonVm> ReCalcurated;
		private void RaiseReCalcurated(CommonVm src) => ReCalcurated?.Invoke(src);
		public void ReCalcurate() {
			this.Root().RaiseReCalcurated(this);
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
		public string AmountRate {
			get { return this.IsRoot() ? "-" : CoreData.AmountRate.ToString("0.#"); }
			set { CoreData.AmountRate = ResultWithValue.Of<double>(double.TryParse, value).Value; }
		}
	}
	public class FinancialVm : CommonVm {
		public FinancialVm(CommonNode model) : base(model) {
		}
	}
	public class BasketVm : CommonVm {
		public BasketVm(CommonNode model) : base(model) {
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
				MenuList.Add(new MenuItemVm(vc) { Header = "アカウント追加" });
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
				MenuList.Add(new MenuItemVm(vc) { Header = "ブローカー追加" });
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
			
			if(ty == NodeType.Broker || ty == NodeType.Account) {
				var vc = new ViewModelCommand(() => {
					
					var lst = new double[] { model.Amount, model.InvestmentValue }
						.Concat(model.Levelorder().OfType<FinancialProduct>().Select(a => (double)a.Quantity))
						.Concat(model.Levelorder().OfType<FinancialProduct>().Select(a => (double)a.TradeQuantity));
					if (lst.All(a => a == 0)) {
						var d = this.Model.Upstream().OfType<TotalRiskFundNode>().LastOrDefault()?.CurrentDate;
						this.Model.Parent.RemoveChild(this.Model);
						HistoryIO.SaveRoots((DateTime)d);
					}else {
						MessageBox.Show("ポジションまたは取引に関するデータを保持しているため削除できません","削除不可",MessageBoxButton.OK,MessageBoxImage.Information);
					}

				});
				MenuList.Add(new MenuItemVm(vc) { Header = "削除" });
			}
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
	public class ProductVm : BasketVm {
		public ProductVm(CommonNode model) : base(model) {
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
