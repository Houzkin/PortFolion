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
using System.Windows;
using System.Collections.Specialized;
using Livet.Messaging;
using System.Data;
using ExpressionEvaluator;

namespace PortFolion.ViewModels {
	public static class ExpParse {
		public static double Try(string exp) {
			if (string.IsNullOrEmpty(exp)) return 0;
			return ResultWithValue.Of<double>(double.TryParse, exp).TrueOrNot(
				o => o,
				x => {
					try {
						var expression = new CompiledExpression<double>(exp);
						var result = expression.Eval();
						return result;
					} catch {
						return 0;
					}
				});
		}
		
	}

	public class AccountEditVM : DynamicViewModel<AccountNode> {
		private InteractionMessenger _messenger;
		public InteractionMessenger Messenger {
			get { return _messenger = _messenger ?? new InteractionMessenger(); }
			set { _messenger = value; }
		}
		public AccountEditVM(AccountNode an) : base(an) {
			var cash = Model.GetOrCreateNuetral();
			resetElements();
			Elements.CollectionChanged += (s, e) => ChangedTemporaryAmount();
			DummyStock = new StockEditVM(this);
			DummyProduct = new ProductEditVM(this);
		}
		NodeNameEditerVM nne;
		public NodeNameEditerVM NodeNameEditer {
			get { return nne; }
			set {
				if (SetProperty(ref nne, value)) {
					if (nne == null)
						Messenger.Raise(new InteractionMessage("EditEndNodeName"));
					else
						Messenger.Raise(new InteractionMessage("EditNodeName"));
				}
			}
		}
		public string StatusComment { get; private set; }
		public void SetStatusComment(string comment) {
			StatusComment = comment;
			OnPropertyChanged(nameof(StatusComment));
		}
		public new AccountNode Model => base.Model;
		public CashEditVM CashElement => Elements.First(a => a.IsCash);
		public ObservableCollection<CashEditVM> Elements { get; } = new ObservableCollection<CashEditVM>();
		public DateTime CurrentDate
			=> (Model.Root() as TotalRiskFundNode).CurrentDate;

		public string TemporaryAmount
			=> Elements.Sum(a => ExpParse.Try(a.Amount)).ToString("#,#.##");

		public void ChangedTemporaryAmount()
			=> OnPropertyChanged(nameof(TemporaryAmount));

		#region add stock
		StockEditVM _dummyStock;
		public StockEditVM DummyStock {
			get { return _dummyStock; }
			set {
				if (_dummyStock == value) return;
				var tmp = _dummyStock as INotifyDataErrorInfo;
				if(tmp != null) tmp.ErrorsChanged -= TempS_ErrorsChanged;
				_dummyStock = value;
				tmp = _dummyStock as INotifyDataErrorInfo;
				if (tmp != null) tmp.ErrorsChanged += TempS_ErrorsChanged;
				OnPropertyChanged(nameof(DummyStock));
			}
		}
		private void TempS_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
			=> AddStock.RaiseCanExecuteChanged();

		ViewModelCommand addStockCmd;
		public ViewModelCommand AddStock 
			=> addStockCmd = addStockCmd ?? new ViewModelCommand(executeAddStock, canAddStock);
		bool canAddStock() {
			return !DummyStock.HasErrors;// && Elements.All(a => a.Name != DummyStock.Name);//&& Elements.Where(a=>a.IsStock).All(a => a.Name != DummyStock.Name);
		}
		void executeAddStock() {
			DummyStock.Apply();
			Elements.Insert(0, DummyStock);
			DummyStock = new StockEditVM(this);
		}
		ViewModelCommand clearNewStockParams;
		public ICommand ClearNewStockParams =>
			clearNewStockParams = clearNewStockParams ?? new ViewModelCommand(() => DummyStock = new StockEditVM(this));

		#endregion add stock

		#region add product
		ProductEditVM _dummyProduct;
		public ProductEditVM DummyProduct {
			get { return _dummyProduct; }
			set {
				if (_dummyProduct == value) return;
				var tmp = (_dummyProduct as INotifyDataErrorInfo);
				if(tmp != null) tmp.ErrorsChanged -= TmpP_ErrorsChanged;
				_dummyProduct = value;
				tmp = _dummyProduct;
				if (tmp != null) tmp.ErrorsChanged += TmpP_ErrorsChanged;
				OnPropertyChanged(nameof(DummyProduct));
			}
		}
		private void TmpP_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
			=> AddProduct.RaiseCanExecuteChanged();

		ViewModelCommand addProductCommand;
		public ViewModelCommand AddProduct
			=> addProductCommand = addProductCommand ?? new ViewModelCommand(executeAddProduct, canAddProduct);
		bool canAddProduct() {
			return !DummyProduct.HasErrors;// && Elements.Where(a=>a.IsProduct).All(a => a.Name != DummyProduct.Name);
		}
		void executeAddProduct() {
			DummyProduct.Apply();
			Elements.Insert(0, DummyProduct);
			DummyProduct = new ProductEditVM(this);
		}
		ViewModelCommand clearNewProductParams;
		public ICommand ClearNewProductParams =>
			clearNewProductParams = clearNewProductParams ?? new ViewModelCommand(() => DummyProduct = new ProductEditVM(this));
		#endregion add product

		public HashSet<DateTime> EdittingList { get; } = new HashSet<DateTime>();

		ViewModelCommand applyCmd;
		public ICommand Apply => applyCmd = applyCmd ?? new ViewModelCommand(apply, canApply);
		bool canApply()
			=> Elements.All(a => !a.HasErrors);
		void apply() {
			var elems = Elements.Where(a => !a.IsRemoveElement || Model.Children.Contains(a.Model)).ToArray();
			var csh = elems.Where(a => a.IsCash);
			var stc = elems.Where(a => a.IsStock).OfType<StockEditVM>().OrderBy(a => a.Code);
			var prd = elems.Where(a => a.IsProduct).OrderBy(a => a.Name);

			var ary = stc.Concat(prd).Concat(csh);//csh.Concat(stc).Concat(prd).ToArray();
			var rmv = Model.Children.Except(ary.Select(a => a.Model));

			rmv.ForEach(a => Model.Children.Remove(a));
			ary.ForEach((ele, idx) => {
				if (Model.Children.Contains(ele.Model)) {
					int oidx = Model.Children.IndexOf(ele.Model);
					if (oidx != idx)
						Model.Children.Move(oidx, idx);
				} else {
					Model.Children.Insert(idx, ele.Model);
				}
			});
			ary.ForEach(e => e.Apply());
			this.EdittingList.Add(CurrentDate);
			this.Messenger.Raise(new InteractionMessage("CloseAsTrue"));
		}

		ViewModelCommand allSellCmd;
		public ICommand AllSell => allSellCmd = allSellCmd ?? new ViewModelCommand(allsell);
		void allsell() {
			var elem = Elements.OfType<ProductEditVM>();
			var ec = Elements.First(a => a.IsCash);

			var unrePL = elem.Sum(a => ExpParse.Try(a.Amount)); //ResultWithValue.Of<double>(double.TryParse, a.Amount).Value);
			var cam = ExpParse.Try(ec.Amount);//ResultWithValue.Of<double>(double.TryParse, ec.Amount).Value;
			var civ = ExpParse.Try(ec.InvestmentValue);//ResultWithValue.Of<double>(double.TryParse, ec.InvestmentValue).Value;
			ec.InvestmentValue = ((-1D * civ) + (-1D * cam)).ToString();
			ec.Amount = "0";

			foreach(var e in elem) {
				var am = ExpParse.Try(e.Amount);//ResultWithValue.Of<double>(double.TryParse, e.Amount).Value;
				var iv = ExpParse.Try(e.InvestmentValue);//ResultWithValue.Of<double>(double.TryParse, e.InvestmentValue).Value;
				var q = ExpParse.Try(e.Quantity);//ResultWithValue.Of<double>(double.TryParse, e.Quantity).Value;
				var tq = ExpParse.Try(e.TradeQuantity);//ResultWithValue.Of<double>(double.TryParse, e.TradeQuantity).Value;
				e.TradeQuantity = ((-1D * tq) + (-1D * q)).ToString();
				e.InvestmentValue = ((-1D * iv) + (-1D * am)).ToString();
				e.Quantity = "0";
				e.Amount = "0";
			}
			//this.EdittingList.Add(CurrentDate);
			//if(canApply()) apply();
		}

		ViewModelCommand resetCmd;
		public ICommand Reset => resetCmd = resetCmd ?? new ViewModelCommand(resetElements);
		void resetElements() {
			Elements.Clear();
			Model.Children.Select(a => {
				var t = a.GetType();
				if (t == typeof(StockValue)) return new StockEditVM(this, a as StockValue);
				else if (t == typeof(FinancialProduct)) return new ProductEditVM(this, a as FinancialProduct);
				else return new CashEditVM(this, a as FinancialValue);
			}).ForEach(a => Elements.Add(a));
		}
		ViewModelCommand applyCurrentPerPrice;
		public ViewModelCommand ApplyCurrentPerPrice {
			get {
				if(applyCurrentPerPrice == null) {
					applyCurrentPerPrice = new ViewModelCommand(applyCurrentPrice, canApplyCurrentPrice);
					Elements.CollectionChanged += (o, e) => applyCurrentPerPrice.RaiseCanExecuteChanged();
				}
				return applyCurrentPerPrice;
			}
		}
		void applyCurrentPrice() {
			var pfs = Elements.OfType<StockEditVM>();
			if (!pfs.Any()) return;
			var ary = Web.KdbDataClient.AcqireStockInfo(this.CurrentDate).ToArray();
			var dic = new List<Tuple<string, string>>();
			
			foreach(var p in pfs) {
				var sym = ary.Where(a => a.Symbol == p.Code).OrderBy(a => a.Turnover).LastOrDefault();
				if (sym == null) {
					dic.Add(new Tuple<string, string>(p.Code, p.Name));
				} else {
					p.CurrentPerPrice = sym.Close.ToString("#.##");
				}
			}
			if (dic.Any()) {
				string msg = "以下の銘柄は値を更新できませんでした。";
				var m = dic.Aggregate(msg, (seed, ele) => seed + "\n" + ele.Item1 + " - " + ele.Item2);
				MessageBox.Show(m, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			//this.EdittingList.Add(CurrentDate);
		}
		bool canApplyCurrentPrice()
			=> Elements.Where(a => a.IsStock).All(a => !a.HasErrors);
	}
	
	public class CashEditVM : DynamicViewModel<FinancialValue> {
		protected readonly AccountEditVM AccountVM;
		public CashEditVM(AccountEditVM ac, FinancialValue fv) : base(fv) {
			AccountVM = ac;
			_name = fv.Name;
			_InvestmentValue = fv.InvestmentValue.ToString();
			_Amount = fv.Amount.ToString();
			MenuList.Add(new MenuItemVm(editName) { Header = "名前の変更" });
			MenuList.Add(new MenuItemVm(del) { Header = "削除" });
		}
		void editName() {
			var edi = new FromAccountEditerNameEditVM(AccountVM, Model);// new NodeNameEditerVM(Model.Parent, Model);
			AccountVM.NodeNameEditer = edi;
		}
		void del() {
			if (IsCash) {
				MessageBox.Show("このポジションは削除できません。", "削除不可", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			if (Model.IsRoot()) {
				if (this.IsRemoveElement) {
					AccountVM.Elements.Remove(this);
					AccountVM.EdittingList.Add(AccountVM.CurrentDate);
				} else {
					MessageBox.Show("ポジションまたは取引に関するデータを保持しているため削除できません。", "削除不可", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}else {
				var elems = RootCollection.GetNodeLine(Model.Path, AccountVM.CurrentDate)
					.Select(a => new { Key = a.Key, Value = a.Value as FinancialProduct })
					.LastOrDefault(a => a.Value != null && a.Key < AccountVM.CurrentDate)?.Value;
				if(elems == null || (elems.Amount == 0 && elems.Quantity == 0 && elems.TradeQuantity == 0 && elems.InvestmentValue == 0)) {
					AccountVM.Elements.Remove(this);
					AccountVM.EdittingList.Add(AccountVM.CurrentDate);
				} else {
					MessageBox.Show("前回の記入項目から継続するポジションは削除できません。\n数量と評価額を「０」にした場合、次回の書き込み時に消滅または削除が可能となります。", "削除不可", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
		}
		public virtual void Apply() {
			Model.Name = Name.Trim();
			Model.SetInvestmentValue((long)_investmentValue);
			Model.SetAmount((long)_amount);
		}
		public bool IsCash => GetType() == typeof(CashEditVM);
		public bool IsStock => GetType() == typeof(StockEditVM);
		public bool IsProduct => GetType() == typeof(ProductEditVM);
		public virtual bool IsRemoveElement => false;
		public new FinancialValue Model => base.Model;
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();
		string _name="";
		[ReflectReferenceValue]
		public string Name {
			get { return _name; }
			set { SetProperty(ref _name, value, nameVali); }
		}
		string nameVali(string param) {
			var nm = param.Trim();
			if (AccountVM.Elements.Contains(this)) {
				if (1 < AccountVM.Elements.Count(a => a.Name == nm))
					return "重複があります";
			} else {
				if (AccountVM.Elements.Any(a => a.Name == nm))
					return "重複があるため追加不可";
			}
			return null;
		}
		protected string _InvestmentValue="";
		protected double _investmentValue => ExpParse.Try(_InvestmentValue);
		public virtual string InvestmentValue {
			get { return _InvestmentValue; }
			set {
				if(SetProperty(ref _InvestmentValue, value)) {
					OnPropertyChanged(nameof(IsRemoveElement));
					if(_amount == 0)
						Amount = (Model.Amount + _investmentValue).ToString("#.##");
				}
			}
		}
		protected string _Amount;
		protected double _amount => ExpParse.Try(_Amount);
		public virtual string Amount {
			get { return _Amount; }
			set {
				if (SetProperty(ref _Amount, value)) {
					OnPropertyChanged(nameof(IsRemoveElement));
					if (AccountVM.Elements.Contains(this)) AccountVM.ChangedTemporaryAmount();
				}
			}
		}
		
		public virtual bool IsReadOnlyTradeQuantity => true;
		public virtual bool IsReadOnlyQuantity => true;
		public virtual bool IsReadOnlyPerPrice => true;
	}
	public class ProductEditVM : CashEditVM {
		public ProductEditVM(AccountEditVM ac, FinancialProduct fp) : base(ac, fp) {
			_TradeQuantity = fp.TradeQuantity.ToString();
			_CurrentPerPrice = fp.Quantity != 0 ? (fp.Amount / fp.Quantity).ToString("#.##") : "0";
			_Quantity = fp.Quantity.ToString();
		}
		public override void Apply() {
			base.Apply();
			Model.SetTradeQuantity((long)_tradeQuantity);
			Model.SetQuantity((long)_quantity);
		}
		public ProductEditVM(AccountEditVM ac) : base(ac, new FinancialValue()) { }
		public override bool IsRemoveElement => _amount == 0 && _quantity == 0 && _investmentValue == 0 && _tradeQuantity == 0;
		public new FinancialProduct Model => base.Model as FinancialProduct;
		public override string InvestmentValue {
			get { return base.InvestmentValue; }
			set {
				if (SetProperty(ref _InvestmentValue, value)) {
					OnPropertyChanged(nameof(IsRemoveElement));
				}
				//if (SetProperty(ref _InvestmentValue, value)) {
				//	Amount = (Model.Amount + (_tradeQuantity * _investmentValue)).ToString();
				//}
			}
		}
		public override bool IsReadOnlyTradeQuantity => true;// false;
		protected string _TradeQuantity="";
		protected double _tradeQuantity => ExpParse.Try(_TradeQuantity);
		public virtual string TradeQuantity {
			get { return _TradeQuantity; }
			set {
				if(SetProperty(ref _TradeQuantity, value)) {
					OnPropertyChanged(nameof(IsRemoveElement));
				Quantity = (Model.Quantity + _tradeQuantity).ToString("#.##");
				}
			}
		}
		public override bool IsReadOnlyPerPrice => true;// false;
		protected string _CurrentPerPrice="0";
		protected double _currentPerPrice => ExpParse.Try(_CurrentPerPrice);
		public virtual string CurrentPerPrice {
			get { return _CurrentPerPrice; }
			set {
				if(SetProperty(ref _CurrentPerPrice, value)) {
					Amount = (_quantity * _currentPerPrice).ToString();
				}
			}
		}
		public override bool IsReadOnlyQuantity => true;// false;
		protected string _Quantity="0";
		protected double _quantity => ExpParse.Try(_Quantity);
		public virtual string Quantity {
			get { return _Quantity; }
			set {
				if (SetProperty(ref _Quantity, value)) {
					OnPropertyChanged(nameof(IsRemoveElement));
					Amount = (_quantity * _currentPerPrice).ToString();
				}
			}
		}
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
			set {
				if (SetProperty(ref _Code, value, codeValidate))
					AccountVM.ApplyCurrentPerPrice.RaiseCanExecuteChanged();
			}
		}
		string codeValidate(string value) {
			var r = ResultWithValue.Of<int>(int.TryParse, value);
			if (!r) return "コードを入力してください";
			if (value.Count() != 4) return "4桁";
			//var d = this.AccountVM.CurrentDate;
			//var cm = setNameAndPrice(r.Value, d);
			//this.AccountVM.SetStatusComment(cm);
			return null;
		}
		string setNameAndPrice(int r, DateTime d) {
			this.AccountVM.SetStatusComment("コード: " + Code + " の銘柄情報を取得開始します");
			IEnumerable<StockInfo> siis = Enumerable.Empty<StockInfo>();
			try {
				siis = Web.KdbDataClient.AcqireStockInfo(d).Where(a => int.Parse(a.Symbol) == r).ToArray();
			} catch {
				return "エラーにより取得不可";
			} finally { }

			StockInfo si = null;
			if (!siis.Any()) {
				return "指定したコードの銘柄は存在しません";
			}else {
				si = siis.OrderBy(a => a.Turnover).Last();
			}
			this.Name = si.Name;
			this.OnPropertyChanged(nameof(Name));
			if(si.Turnover != 0) {
				this.CurrentPerPrice = si.Close.ToString("#.##");
				return this.Name + "の終値を適用しました";
			}else {
				return this.Name + "は出来高がないため終値を取得できませんでした";
			}

		}
		public override void Apply() {
			base.Apply();
			Model.Code = ResultWithValue.Of<int>(int.TryParse, _Code).Value;
		}
		ViewModelCommand applySymbolCmd;
		public ViewModelCommand ApplySymbol
			=> applySymbolCmd = applySymbolCmd ?? new ViewModelCommand(applySymbol, canApplySymbol);

		void applySymbol() 
			=> ResultWithValue.Of<int>(int.TryParse, Code)
				.TrueOrNot(o => AccountVM.SetStatusComment(setNameAndPrice(o, AccountVM.CurrentDate)));

		bool canApplySymbol()
			=> string.IsNullOrEmpty(codeValidate(this.Code));
	}
}
