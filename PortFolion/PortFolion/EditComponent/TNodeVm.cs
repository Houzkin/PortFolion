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
	public class VmCoreBase : BindableObject {
		public VmCoreBase() : base(new object()) { }
		public VmCoreBase(CommonNode node) : base(node) { }
		public virtual void Copy(VmCoreGeneral core) {
			_invTtl = core.InvestmentTotal;
			_invRTtl = core.InvestmentReturnTotal;
			_ar = core.AmountRate;
		}
		public DateTime? CurrentDate { get; set; }
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
		double _ar;
		public double AmountRate {
			get { return _ar; }
			set {
				if (_ar == value) return;
				_ar = value;
				OnPropertyChanged();
			}
		}
		#endregion

	}
	public class VmCoreBasket : VmCoreBase {
		public VmCoreBasket() { }
		public VmCoreBasket(CommonNode node) : base(node) { }
		public override void Copy(VmCoreGeneral core) {
			base.Copy(core);
			_pl = core.ProfitLoss;
			_upl = core.UnrealizedProfitLoss;
			_uplr = core.UnrealizedPLRatio;
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
		public override void Copy(VmCoreGeneral core) {
			base.Copy(core);
			_pp = core.PerPrice;
			_pbpa = core.PerBuyPriceAverage;
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
		public static IEnumerable<VmCoreBase> ReCalcHistory(IEnumerable<string> path) {
			var nd = RootCollection.GetNodeLine(path)
				.Select(a => recalc(Create(a.Value).Levelorder().Reverse(), a.Key).Last().ToHistoryVm());
			return nd;
		}
		//class Scanable {
		//	public VmCoreGeneral Current { get; set; }
		//	public VmCoreGeneral Previous { get; set; }
		//}
		//public static IEnumerable<CommonVm> _ReCalcHistory(IEnumerable<string> path) {
		//	var nd = RootCollection.GetNodeLine(path).Values
		//		.Select(a => Create(a))
		//		.Scan(new Scanable(), (pre, cur) => {
		//			var sbl = new Scanable();
		//			sbl.Current.InvestmentTotal = pre.Current.InvestmentTotal + cur.InvestmentTotal;
		//			sbl.Current.InvestmentReturnTotal = pre.Current.InvestmentReturnTotal + cur.InvestmentReturnTotal;
		//			if (!cur.Model.IsRoot()) { sbl.Current.AmountRate = cur.Model.Amount / cur.Model.Parent.Amount * 100; }
		//			//
		//			return sbl;
		//		});
		//	throw new NotImplementedException();
		//}
		public static void ReCalcurate(CommonNodeVM src,DateTime d) {
			var refList = src.Levelorder().Skip(1).Reverse()
				.Concat(src.Siblings())
				.Concat(src.Upstream().Skip(1));
			recalc(refList, d);
		}
		static IEnumerable<CommonNodeVM> recalc(IEnumerable<CommonNodeVM> refList,DateTime d) {
			foreach (var nd in refList) {
				var cpl = RootCollection.GetNodeLine(nd.Path, d)
					.Where(a => a.Key <= d)
					.ToDictionary(a => a.Key, a => a.Value);
				calcCommon(cpl, nd);
				if (nd.GetType() == typeof(FinancialBasketVM)) calcBasket(nd as FinancialBasketVM);
				else if (nd.GetType () == typeof(FinancialProductVM)) calcProduct(cpl, nd as FinancialProductVM);
				if((nd as FinancialBasketVM) != null)
					nd.CoreData.UnrealizedPLRatio = nd.Model.Amount != 0 ? nd.CoreData.UnrealizedProfitLoss / nd.Model.Amount * 100 : 0;
			}
			return refList;
		}
		static void calcCommon(Dictionary<DateTime,CommonNode> dic, CommonNodeVM vm) {
			vm.CoreData.InvestmentTotal = dic.Where(a => 0 < a.Value.InvestmentValue).Sum(a => a.Value.InvestmentValue);
			vm.CoreData.InvestmentReturnTotal = Math.Abs(dic.Where(a => 0 > a.Value.InvestmentValue).Sum(a => a.Value.InvestmentValue));
			if (!vm.Model.IsRoot()) {
				double v = ((double)vm.Model.Amount / (double)vm.Model.Parent.Amount) * 100;
				vm.CoreData.AmountRate = v;
			}
		}
		static void calcBasket(FinancialBasketVM vm) {
			vm.CoreData.ProfitLoss = vm.Model.Amount - vm.CoreData.InvestmentTotal - vm.CoreData.InvestmentReturnTotal;
			vm.CoreData.UnrealizedProfitLoss = vm.Preorder().OfType<FinancialProductVM>().Sum(a => a.UnrealizedProfitLoss);//vm.Children.OfType<BasketVm>().Sum(a => a.UnrealizedProfitLoss);
		}
		static void calcProduct(Dictionary<DateTime,CommonNode> dic, FinancialProductVM vm) {
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
		protected CommonNodeVM(CommonNode model) : base(model) {
			CoreData.PropertyChanged += this.corePropertyChanged;
		}
		protected override CommonNodeVM GenerateChild(CommonNode modelChildNode) {
			return Create(modelChildNode);
		}
		public virtual VmCoreBase ToHistoryVm() {
			var cb = new VmCoreBase(this.Model);
			cb.Copy(CoreData);
			cb.CurrentDate = this.CurrentDate;
			return cb;
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
		public string AmountRate {
			get { return this.IsRoot() ? "-" : CoreData.AmountRate.ToString("0.#"); }
			set { CoreData.AmountRate = ResultWithValue.Of<double>(double.TryParse, value).Value; }
		}
	}
	public class FinancialValueVM : CommonNodeVM {
		public FinancialValueVM(CommonNode model) : base(model) {
		}
	}
	public class FinancialBasketVM : CommonNodeVM {
		public FinancialBasketVM(CommonNode model) : base(model) {
			var sp = new ViewModelCommand(() => DisplayHistory());
			//var uri = new Uri("PortFolion;component/Resources/Icons.xaml", UriKind.Relative);
			//StreamResourceInfo info = Application.GetResourceStream(uri);
			//var  reader = new System.Windows.Markup.XamlReader();
			//var dictionary = reader.LoadAsync(info.Stream) as ResourceDictionary;
			//var element = dictionary["appbar_add"] as BitmapImage ;
			//var img = new Image();
			//img.Source = element;Icon = img 
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
		public override VmCoreBase ToHistoryVm() {
			var cb = new VmCoreBasket(this.Model);
			cb.Copy(CoreData);
			cb.CurrentDate = this.CurrentDate;
			return cb;
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
	public class FinancialProductVM : FinancialBasketVM {
		public FinancialProductVM(CommonNode model) : base(model) {
		}
		public override VmCoreBase ToHistoryVm() {
			var cb = new VmCoreGeneral(this.Model);
			cb.Copy(CoreData);
			cb.CurrentDate = this.CurrentDate;
			return cb;
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
