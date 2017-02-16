using Houzkin;
using Houzkin.Architecture;
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
		CashVM cashVM;
		public AccountEditVM(AccountNode an) : base(an) {
			var cash = Model.Children.FirstOrDefault(a => a.GetType() == typeof(FinancialValue)) as FinancialValue;
			if(cash == null) {
				cash = new FinancialValue();
				cash.Name = "余力";
				Model.AddChild(cash);
			}
			cashVM = new CashVM(cash);
			Stocks = new ObservableCollection<StockVM>(
				Model.Children
					.Where(a => a.GetType() == typeof(StockValue))
					.Select(a => new StockVM(a as StockValue)));
			Products = new ObservableCollection<ProductVM>(
				Model.Children
					.Where(a => a.GetType() == typeof(FinancialProduct))
					.Select(a => new ProductVM(a as FinancialProduct)));
		}
		public ObservableCollection<StockVM> Stocks { get; private set; }
		public ObservableCollection<ProductVM> Products { get; private set; }

		public StockVM DummyStock { get; } = new StockVM();
		ViewModelCommand addStockCmd;
		public ICommand AddStock 
			=> addStockCmd = addStockCmd ?? new ViewModelCommand(executeAddStock, canAddStock);
		bool canAddStock() {
			return DummyStock.CanCreateNewViewModel && Stocks.All(a => a.Name != DummyStock.Name);
		}
		void executeAddStock() {
			Stocks.Add(DummyStock.CreateViewModel());
		}

		public ProductVM DummyProduct { get; } = new ProductVM();
		ViewModelCommand addProductCommand;
		public ICommand AddProduct
			=> addProductCommand = addProductCommand ?? new ViewModelCommand(executeAddProduct, canAddProduct);
		bool canAddProduct() {
			return DummyProduct.CanCreateNewViewModel && Products.All(a => a.Name != DummyProduct.Name);
		}
		void executeAddProduct() {
			Products.Add(DummyProduct.CreateViewModel());
		}

		ViewModelCommand applyCmd;
		public ICommand Apply => applyCmd = applyCmd ?? new ViewModelCommand(apply, canApply);
		bool canApply() 
			=> Stocks.All(a => !a.HasErrors) && Products.All(a => !a.HasErrors) && !cashVM.HasErrors;
		
		void apply() {

		}
	}
	
	public class CashVM : DynamicViewModel<FinancialValue> {
		public CashVM(FinancialValue fv) : base(fv) {
			if (fv != null) {
				Name = fv.Name;
				_InvestmentValue = fv.InvestmentValue.ToString();
				_Amount = fv.Amount.ToString();
			}
		}
		protected bool IsDummy => Model == null;
		string _name;
		public string Name {
			get { return _name; }
			set { SetProperty(ref _name, value); }
		}
		protected string _InvestmentValue;
		public virtual string InvestmentValue {
			get { return _InvestmentValue; }
			set {
				if(SetProperty(ref _InvestmentValue, value)) {
					Amount = Model.Amount + _InvestmentValue;
				}
			}
		}
		protected string _Amount;
		public virtual string Amount {
			get { return _Amount; }
			set { SetProperty(ref _Amount, value); }
		}
	}
	public class ProductVM : CashVM {
		public ProductVM(FinancialProduct fp) : base(fp) { }
		public ProductVM() : base(null) { }
		public new FinancialProduct Model => base.Model as FinancialProduct;
		protected string _Quantity;
		public string Quantity { get; set; }
		protected string _TradeQuantity;
		public string TradeQuantity {
			get { return _TradeQuantity; }
			set {
				if(SetProperty(ref _TradeQuantity, value)) {

				}
			}
		}
		protected string _CurrentPerPrice;
		public string CurrentPerPrice { get; set; }

		void setValue(long tQuantity,long invest,long quantity,long amount,double perValue) {
			//amount
			if (amount == 0) {
				var am = (long)((tQuantity + quantity) * perValue);
				if (am != 0) amount = am;
			}
			//perValue
			else if(perValue == 0) {
				var q = tQuantity + quantity;
				if (q != 0) {
					perValue = amount / (tQuantity + quantity);
				}
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
		public StockVM(StockValue sv) : base(sv) { }
		public StockVM() : base() { }
		public new StockValue Model => base.Model as StockValue;
		public string Code { get; set; }

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
