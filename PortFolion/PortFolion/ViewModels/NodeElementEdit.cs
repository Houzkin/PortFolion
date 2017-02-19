using Houzkin;
using Houzkin.Architecture;
using Houzkin.Tree;
using Livet;
using Livet.Commands;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PortFolion.ViewModels {
	public class AccountEditVM : DynamicViewModel<AccountNode> {
		public AccountEditVM(AccountNode an) : base(an) {
			var cash = Model.Children.FirstOrDefault(a => a.GetType() == typeof(FinancialValue)) as FinancialValue;
			if(cash == null) {
				cash = new FinancialValue();
				cash.Name = "余力";
				Model.AddChild(cash);
			}
			
			Elements = new ObservableCollection<CashVM>(
				Model.Children.Select(a => {
					var t = a.GetType();
					if (t == typeof(StockValue)) return new StockVM(a as StockValue);
					else if (t == typeof(FinancialProduct)) return new ProductVM(a as FinancialProduct);
					else return new CashVM(a as FinancialValue);
				}));

		}
		public ObservableCollection<CashVM> Elements { get; set; }

		public StockVM DummyStock { get; } = new StockVM();
		ViewModelCommand addStockCmd;
		public ICommand AddStock 
			=> addStockCmd = addStockCmd ?? new ViewModelCommand(executeAddStock, canAddStock);
		bool canAddStock() {
			return DummyStock.CanCreateNewViewModel && Elements.Where(a=>a.IsStock).All(a => a.Name != DummyStock.Name);
		}
		void executeAddStock() {
			Elements.Add(DummyStock.CreateViewModel());
		}

		public ProductVM DummyProduct { get; } = new ProductVM();
		ViewModelCommand addProductCommand;
		public ICommand AddProduct
			=> addProductCommand = addProductCommand ?? new ViewModelCommand(executeAddProduct, canAddProduct);
		bool canAddProduct() {
			return DummyProduct.CanCreateNewViewModel && Elements.Where(a=>a.IsProduct).All(a => a.Name != DummyProduct.Name);
		}
		void executeAddProduct() {
			Elements.Add(DummyProduct.CreateViewModel());
		}

		ViewModelCommand applyCmd;
		public ICommand Apply => applyCmd = applyCmd ?? new ViewModelCommand(apply, canApply);
		bool canApply()
			=> Elements.All(a => !a.HasErrors);
		void apply() {
			int cnt = Elements.Count;
			for(int i = 0; i<cnt; i++) {

			}
			Elements.Where(a => !a.IsRemoveElement || Model.Children.Contains(a.Model)).ForEach((ele, index) => {
				if (Model.Children[index] == ele.Model) return;
				if (Model.Children.Contains(ele.Model)) {
					int oldIndex = Model.Children.IndexOf(ele.Model);
					Model.Children.Move(oldIndex, index);
					return;
				}
				if (ele.IsCash) {
					Model.Children.Insert(0, ele.Model);
				} else if (ele.IsProduct) {
					var li = Model.Children.Where(a => a.GetType() == typeof(FinancialProduct)).OrderBy(a => a.Name).LastOrDefault();
					if (li == null) {
						Model.Children.Add(ele.Model);
					} else {
						int lidx = Model.Children.IndexOf(li);
						Model.Children.Insert(lidx + 1, ele.Model);
					}
				} else if (ele.IsStock) {
					var li = Model.Children.OfType<StockValue>().OrderBy(a => a.Code).LastOrDefault();
					if (li == null) {
						Model.Children.Insert(1, ele.Model);
					} else {
						int lidx = Model.Children.IndexOf(li);
						Model.Children.Insert(lidx + 1, ele.Model);
					}
				}
			});
		}

		ViewModelCommand allSellCmd;
		public ICommand AllSell => allSellCmd = allSellCmd ?? new ViewModelCommand(allsell);
		void allsell() {
			var elem = Elements.OfType<ProductVM>();
			var ec = Elements.Single(a => a.IsCash);

			var unrePL = elem.Sum(a => ResultWithValue.Of<double>(double.TryParse, a.Amount).Value);
			var cam = ResultWithValue.Of<double>(double.TryParse, ec.Amount).Value;
			var civ = ResultWithValue.Of<double>(double.TryParse, ec.InvestmentValue).Value;
			ec.InvestmentValue = ((-1D * civ) + (-1D * cam)).ToString();
			ec.Amount = "0";

			foreach(var e in elem) {
				var am = ResultWithValue.Of<double>(double.TryParse, e.Amount).Value;
				var iv = ResultWithValue.Of<double>(double.TryParse, e.InvestmentValue).Value;
				var q = ResultWithValue.Of<double>(double.TryParse, e.Quantity).Value;
				var tq = ResultWithValue.Of<double>(double.TryParse, e.TradeQuantity).Value;
				e.TradeQuantity = ((-1D * tq) + (-1D * q)).ToString();
				e.InvestmentValue = ((-1D * iv) + (-1D * am)).ToString();
				e.Quantity = "0";
				e.Amount = "0";
			}
			if(canApply()) apply();
		}
	}
	
	public class CashVM : DynamicViewModel<FinancialValue> {
		public CashVM(FinancialValue fv) : base(fv) {
			Name = fv.Name;
			_InvestmentValue = fv.InvestmentValue.ToString();
			_Amount = fv.Amount.ToString();
		}
		protected CashVM() : base(null) { }
		public bool IsCash => GetType() == typeof(CashVM);
		public bool IsStock => GetType() == typeof(StockVM);
		public bool IsProduct => GetType() == typeof(ProductVM);
		public virtual bool IsRemoveElement => false;
		public new FinancialValue Model => base.Model;
		string _name;
		public string Name {
			get { return _name; }
			set { SetProperty(ref _name, value); }
		}
		protected string _InvestmentValue;
		protected double _investmentValue => ResultWithValue.Of<double>(double.TryParse, _InvestmentValue).Value;
		public virtual string InvestmentValue {
			get { return _InvestmentValue; }
			set {
				if(SetProperty(ref _InvestmentValue, value)) {
					Amount = Model.Amount + _InvestmentValue;
				}
			}
		}
		protected string _Amount;
		protected double _amount => ResultWithValue.Of<double>(double.TryParse, _Amount).Value;
		public virtual string Amount {
			get { return _Amount; }
			set {
				SetProperty(ref _Amount, value);
				OnPropertyChanged(nameof(IsRemoveElement));
			}
		}
	}
	public class ProductVM : CashVM {
		public ProductVM(FinancialProduct fp) : base(fp) {
			_TradeQuantity = fp.TradeQuantity.ToString();
			_CurrentPerPrice = fp.Quantity != 0 ? (fp.Amount / fp.Quantity).ToString() : "";
			_Quantity = fp.Quantity.ToString();
		}
		public ProductVM() : base() { }
		public override bool IsRemoveElement => _amount == 0 && _quantity == 0;
		public new FinancialProduct Model => base.Model as FinancialProduct;
		protected string _TradeQuantity;
		protected double _tradeQuantity => ResultWithValue.Of<double>(double.TryParse, _TradeQuantity).Value;
		public virtual string TradeQuantity {
			get { return _TradeQuantity; }
			set {
				if(SetProperty(ref _TradeQuantity, value)) {
					Quantity = (Model.Quantity + _tradeQuantity).ToString();
				}
			}
		}
		public override string InvestmentValue {
			get { return base.InvestmentValue; }
			set { SetProperty(ref _InvestmentValue, value); }
		}
		protected string _CurrentPerPrice;
		protected double _currentPerPrice => ResultWithValue.Of<double>(double.TryParse, _CurrentPerPrice).Value;
		public virtual string CurrentPerPrice {
			get { return _CurrentPerPrice; }
			set {
				if(SetProperty(ref _CurrentPerPrice, value)) {
					Amount = (_quantity * _currentPerPrice).ToString();
				}
			}
		}
		protected string _Quantity;
		protected double _quantity => ResultWithValue.Of<double>(double.TryParse, _Quantity).Value;
		public virtual string Quantity {
			get { return _Quantity; }
			set {
				if(SetProperty(ref _Quantity, value)) 
					Amount = (_quantity * _currentPerPrice).ToString();
			}
		}

		#region dummy's method
		public virtual bool CanCreateNewViewModel {
			get {
				return !string.IsNullOrEmpty(Name)
					&& !string.IsNullOrWhiteSpace(Name)
					&& ResultWithValue.Of<long>(long.TryParse, InvestmentValue)
					&& ResultWithValue.Of<long>(long.TryParse, Amount)
					&& ResultWithValue.Of<long>(long.TryParse, TradeQuantity);
			}
		}
		protected virtual FinancialProduct ToNewViewModel(FinancialProduct fp) {
			fp.Name = this.Name;
			fp.SetAmount(long.Parse(this.Amount));
			fp.SetInvestmentValue(long.Parse(InvestmentValue));
			fp.SetQuantity(long.Parse(Quantity));
			fp.SetTradeQuantity(long.Parse(TradeQuantity));
			return fp;
		}
		public ProductVM CreateViewModel() {
			if (CanCreateNewViewModel)
				return new ProductVM(this.ToNewViewModel(new FinancialProduct()));
			else
				throw new InvalidOperationException();
		}
		#endregion
	}
	public class StockVM: ProductVM {
		public StockVM(StockValue sv) : base(sv) {
			_Code = sv.Code.ToString();
		}
		public StockVM() : base() { }
		public new StockValue Model => base.Model as StockValue;
		protected string _Code;
		public string Code {
			get { return _Code; }
			set { SetProperty(ref _Code, value, codeValidate); }
		}
		string codeValidate(string value) {
			var r = ResultWithValue.Of<int>(int.TryParse, value);
			if (!r) return "コードを入力してください";
			if (value.Count() > 4) return "4桁";
			if (value.Count() < 4) return "";
			var d = Model.Upstream().OfType<TotalRiskFundNode>().Last().CurrentDate;
			var tgh = Web.KdbDataClient.AcqireStockInfo(d)
				.Where(a => int.Parse(a.Symbol) == r.Value).ToArray();
			if (!tgh.Any()) return "";
			this.CurrentPerPrice = tgh.OrderBy(a => a.Turnover).Last().Close.ToString();
			return null;
		}

		#region dummy's method
		public override bool CanCreateNewViewModel
			=> base.CanCreateNewViewModel && ResultWithValue.Of<int>(int.TryParse, Code);

		protected override FinancialProduct ToNewViewModel(FinancialProduct fp) {
			var m = base.ToNewViewModel(fp) as StockValue;
			m.Code = int.Parse(Code);
			return m;
		}
		public new StockVM CreateViewModel() {
			if (CanCreateNewViewModel)
				return new StockVM(ToNewViewModel(new StockValue()) as StockValue);
			else
				throw new InvalidOperationException();
		}
		#endregion
	}
}
