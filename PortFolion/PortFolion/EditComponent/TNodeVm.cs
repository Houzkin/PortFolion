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
	public class VmCoreData : NotificationObject {
		#region common
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
		#endregion
		#region basket
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
		#endregion
		#region Product
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
		#endregion
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
		#endregion
		protected VmCoreData CoreData { get; } = new VmCoreData();
		protected CommonVm(CommonNode model) : base(model) {
			CoreData.PropertyChanged += this.corePropertyChanged;
		}
		protected override CommonVm GenerateChild(CommonNode modelChildNode) {
			return Create(modelChildNode);
		}
		public void SetCore(VmCoreData core) {
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
