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
using System.ComponentModel;

namespace PortFolion.ViewModels {
	public class AccountEditVM : DynamicViewModel<AccountNode> {
		public AccountEditVM(AccountNode an) : base(an) {
			var cash = Model.Children.FirstOrDefault(a => a.GetType() == typeof(FinancialValue)) as FinancialValue;
			if(cash == null) {
				cash = new FinancialValue();
				string name = "余力";
				int num = 1;
				while(Model.Children.Any(a=>a.Name == name)) {
					name = "余力" + num;
					num++;
				}
				cash.Name = name;
				Model.AddChild(cash);
			}
			
			Elements = new ObservableCollection<CashEditVM>(
				Model.Children.Select(a => {
					var t = a.GetType();
					if (t == typeof(StockValue)) return new StockEditVM(this, a as StockValue);
					else if (t == typeof(FinancialProduct)) return new ProductEditVM(this, a as FinancialProduct);
					else return new CashEditVM(this, a as FinancialValue);
				}));
			DummyStock = new StockEditVM(this);
			DummyProduct = new ProductEditVM(this);
		}
		public ObservableCollection<CashEditVM> Elements { get; private set; }

		StockEditVM _dummyStock;
		public StockEditVM DummyStock {
			get { return _dummyStock; }
			set {
				if (_dummyStock == value) return;
				var ndei = (_dummyStock as INotifyDataErrorInfo);
				if(ndei != null) ndei.ErrorsChanged -= Ndei_ErrorsChanged;
				_dummyStock = value;
				ndei = _dummyStock;
				if (ndei != null) ndei.ErrorsChanged += Ndei_ErrorsChanged;
			}
		}
		private void Ndei_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
			=> AddStock.RaiseCanExecuteChanged();

		ViewModelCommand addStockCmd;
		public ViewModelCommand AddStock 
			=> addStockCmd = addStockCmd ?? new ViewModelCommand(executeAddStock, canAddStock);
		bool canAddStock() {
			return !DummyStock.HasErrors && Elements.Where(a=>a.IsStock).All(a => a.Name != DummyStock.Name);
		}
		void executeAddStock() {
			DummyStock.Apply();
			Elements.Add(DummyStock);
			DummyStock = new StockEditVM(this);
			OnPropertyChanged(nameof(DummyStock));
		}

		ProductEditVM _dummyProduct;
		public ProductEditVM DummyProduct {
			get { return _dummyProduct; }
			set {
				if (_dummyProduct == value) return;
				var tmp = (_dummyProduct as INotifyDataErrorInfo);
				if(tmp != null) tmp.ErrorsChanged -= Tmp_ErrorsChanged;
				_dummyProduct = value;
				tmp = _dummyProduct;
				if (tmp != null) tmp.ErrorsChanged += Tmp_ErrorsChanged;
			}
		}
		private void Tmp_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
			=> AddProduct.RaiseCanExecuteChanged();

		ViewModelCommand addProductCommand;
		public ViewModelCommand AddProduct
			=> addProductCommand = addProductCommand ?? new ViewModelCommand(executeAddProduct, canAddProduct);
		bool canAddProduct() {
			return DummyProduct.CanCreateNewViewModel && Elements.Where(a=>a.IsProduct).All(a => a.Name != DummyProduct.Name);
		}
		void executeAddProduct() {
			DummyProduct.Apply();
			Elements.Add(DummyProduct);
			DummyProduct = new ProductEditVM(this);
			OnPropertyChanged(nameof(DummyProduct));
		}

		ViewModelCommand applyCmd;
		public ICommand Apply => applyCmd = applyCmd ?? new ViewModelCommand(apply, canApply);
		bool canApply()
			=> Elements.All(a => !a.HasErrors);
		void apply() {
			var elems = Elements.Where(a => !a.IsRemoveElement || Model.Children.Contains(a.Model)).ToArray();
			var csh = elems.Where(a => a.IsCash);
			var stc = elems.Where(a => a.IsStock).OfType<StockEditVM>().OrderBy(a => a.Code);
			var prd = elems.Where(a => a.IsProduct).OrderBy(a => a.Name);
			csh.Concat(stc).Concat(prd).ForEach((ele, idx) => {
				if (Model.Children.Contains(ele.Model)) {
					int oidx = Model.Children.IndexOf(ele.Model);
					if(oidx != idx)
						Model.Children.Move(oidx, idx);
				}else {
					Model.Children.Insert(idx, ele.Model);
				}
			});
		}

		ViewModelCommand allSellCmd;
		public ICommand AllSell => allSellCmd = allSellCmd ?? new ViewModelCommand(allsell);
		void allsell() {
			var elem = Elements.OfType<ProductEditVM>();
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

		ViewModelCommand cancelCml;
		public ICommand Cancel => cancelCml = cancelCml ?? new ViewModelCommand(cancel);
		void cancel() {
			Elements.Clear();
			Model.Children.Select(a => {
				var t = a.GetType();
				if (t == typeof(StockValue)) return new StockEditVM(this, a as StockValue);
				else if (t == typeof(FinancialProduct)) return new ProductEditVM(this, a as FinancialProduct);
				else return new CashEditVM(this, a as FinancialValue);
			}).ForEach(a => Elements.Add(a));
		}
	}
	
	public class CashEditVM : DynamicViewModel<FinancialValue> {
		protected readonly AccountEditVM AccountVM;
		public CashEditVM(AccountEditVM ac, FinancialValue fv) : base(fv) {
			AccountVM = ac;
			_name = fv.Name;
			_InvestmentValue = fv.InvestmentValue.ToString();
			_Amount = fv.Amount.ToString();
		}
		public virtual void Apply() {
			Model.Name = Name;
			Model.SetInvestmentValue((long)_investmentValue);
			Model.SetAmount((long)_amount);
		}
		public bool IsCash => GetType() == typeof(CashEditVM);
		public bool IsStock => GetType() == typeof(StockEditVM);
		public bool IsProduct => GetType() == typeof(ProductEditVM);
		public virtual bool IsRemoveElement => false;
		public new FinancialValue Model => base.Model;
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();
		string _name;
		public string Name {
			get { return _name; }
			set { SetProperty(ref _name, value,nameVali); }
		}
		string nameVali(string param) {
			if (AccountVM.Elements.Contains(this)) {
				if (1 < AccountVM.Elements.Count(a => a.Name == param))
					return "重複あり";
			}else {
				if (AccountVM.Elements.Any(a => a.Name == param))
					return "重複があるため追加不可";
			}
			return "";
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
	public class ProductEditVM : CashEditVM {
		public ProductEditVM(AccountEditVM ac, FinancialProduct fp) : base(ac, fp) {
			_TradeQuantity = fp.TradeQuantity.ToString();
			_CurrentPerPrice = fp.Quantity != 0 ? (fp.Amount / fp.Quantity).ToString() : "";
			_Quantity = fp.Quantity.ToString();
		}
		public override void Apply() {
			base.Apply();
			Model.SetTradeQuantity((long)_tradeQuantity);
			Model.SetQuantity((long)_quantity);
		}
		public ProductEditVM(AccountEditVM ac) : base(ac, new FinancialValue()) { }
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
		public ProductEditVM CreateViewModel() {
			if (CanCreateNewViewModel)
				return new ProductEditVM(this.ToNewViewModel(new FinancialProduct()));
			else
				throw new InvalidOperationException();
		}
		#endregion
	}
	public class StockEditVM: ProductEditVM {
		public StockEditVM(AccountEditVM ac, StockValue sv) : base(ac, sv) {
			_Code = sv.Code.ToString();
		}
		public StockEditVM(AccountEditVM ac) : base(ac, new StockValue()) { }
		public new StockValue Model => base.Model as StockValue;
		protected string _Code;
		public string Code {
			get { return _Code; }
			set { SetProperty(ref _Code, value, codeValidate); }
		}
		string codeValidate(string value) {
			var r = ResultWithValue.Of<int>(int.TryParse, value);
			if (!r) return "コードを入力してください";
			if (value.Count() != 4) return "4桁";
			var d = Model.Upstream().OfType<TotalRiskFundNode>().Last().CurrentDate;
			var tgh = Web.KdbDataClient
				.AcqireStockInfo(d)
				.Where(a => int.Parse(a.Symbol) == r.Value).ToArray();
			if (!tgh.Any()) return "";
			var tg = tgh.OrderBy(a => a.Turnover).Last();
			this.CurrentPerPrice = tg.Close.ToString();
			if (string.IsNullOrEmpty(this.Name) || string.IsNullOrWhiteSpace(this.Name))
				this.Name = tg.Name;
			return null;
		}
		public override void Apply() {
			base.Apply();
			Model.Code = ResultWithValue.Of<int>(int.TryParse, _Code).Value;
		}

		#region dummy's method
		public override bool CanCreateNewViewModel
			=> base.CanCreateNewViewModel && ResultWithValue.Of<int>(int.TryParse, Code);

		protected override FinancialProduct ToNewViewModel(FinancialProduct fp) {
			var m = base.ToNewViewModel(fp) as StockValue;
			m.Code = int.Parse(Code);
			return m;
		}
		public new StockEditVM CreateViewModel() {
			if (CanCreateNewViewModel)
				return new StockEditVM(ToNewViewModel(new StockValue()) as StockValue);
			else
				throw new InvalidOperationException();
		}
		#endregion
	}
}
