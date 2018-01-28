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
		bool _isLoading;
		public bool IsLoading {
			get { return _isLoading; }
			set { this.SetProperty(ref _isLoading, value); }
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
			var elems = Elements.Where(a => !a.IsEmptyElement || Model.Children.Contains(a.Model)).ToArray();
			var csh = elems.Where(a => a.IsCash);
			var stc = elems.Where(a => a.IsStock).OfType<StockEditVM>().OrderBy(a => a.Code);
			var prd = elems.Where(a => a.IsProduct).OrderBy(a => a.Name);

			var ary = stc.Concat(prd).Concat(csh).ToArray();
			var rmv = Model.Children.Except(ary.Select(a => a.Model)).ToArray();

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

			var unrePL = elem.Sum(a => ExpParse.Try(a.Amount)); 
			var cam = ExpParse.Try(ec.Amount);
			var civ = ExpParse.Try(ec.InvestmentValue);
			ec.InvestmentValue = ((-1D * civ) + (-1D * cam)).ToString();
			ec.Amount = "0";

			foreach(var e in elem) {
				var am = ExpParse.Try(e.Amount);
				var iv = ExpParse.Try(e.InvestmentValue);
				var q = ExpParse.Try(e.Quantity);
				var tq = ExpParse.Try(e.TradeQuantity);
				e.TradeQuantity = ((-1D * tq) + (-1D * q)).ToString();
				e.InvestmentValue = ((-1D * iv) + (-1D * am)).ToString();
				e.Quantity = "0";
				e.Amount = "0";
			}
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
			OnPropertyChanged(nameof(CashElement));
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
		async void applyCurrentPrice() {
			IsLoading = true;
			var r = await ApplyPerPrice();
			IsLoading = false;
			if (r.Any()) {
				string msg = "以下の銘柄は値を更新できませんでした。";
				var m = r.Aggregate(msg, (seed, ele) => seed + "\n" + ele);
				MessageBox.Show(m, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
			} else {
				this.SetStatusComment(this.CurrentDate.ToString("yyyy年M月d日" + "時点での単価を適用しました。"));
			}
		}
		bool canApplyCurrentPrice()
			=> Elements.Where(a => a.IsStock).All(a => !a.HasErrors);

		/// <summary>現在の単価を更新する</summary>
		/// <returns>更新できなかった銘柄リスト</returns>
		public async Task<IEnumerable<string>> ApplyPerPrice() {
			var pfs = Elements.OfType<StockEditVM>();
			if (!pfs.Any()) return Enumerable.Empty<string>();
			//var ary = Web.KdbDataClient.AcqireStockInfo(this.CurrentDate).ToArray();
			var dic = new List<Tuple<string, string>>();
			
			StockInfo[] ary; 
			try {
				ary = await Task.Run(() => Web.DownloadSource.AcqireStockInfo(this.CurrentDate).ToArray());
			} catch {
				return new string[] { "通信状態を確認して再度実行してください。" };
			}
			foreach(var p in pfs) {
				var sym = ary.Where(a => a.Symbol == p.Code).OrderBy(a => a.Turnover).LastOrDefault();
				if (sym == null) {
					dic.Add(new Tuple<string, string>(p.Code, p.Name));
				} else {
					p.CurrentPerPrice = sym.Close.ToString("#.##");
				}
			}
			return dic.Select(a => a.Item1 + " - " + a.Item2);
			
		}
	}
	
	public class CashEditVM : DynamicViewModel<FinancialValue> {
		protected readonly AccountEditVM AccountVM;
		public CashEditVM(AccountEditVM ac, FinancialValue fv) : base(fv) {
			AccountVM = ac;
			_name = fv.Name;
			_InvestmentValue = fv.InvestmentValue.ToString();
			_Amount = fv.Amount.ToString();
			MenuList.Add(new MenuItemVm(editName) { Header = "名前の変更" });
			MenuList.Add(new MenuItemVm(del2) { Header = "削除" });
		}
		void editName() {
			var edi = new FromAccountEditerNameEditVM(AccountVM, Model);
			AccountVM.NodeNameEditer = edi;
		}
		
		void del2() {
			if (IsCash) {
				MessageBox.Show("このポジションは削除できません。", "削除不可", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			Func<IEnumerable<string>> getPath = () => {
				if (Model.IsRoot()) {
					var lst = this.AccountVM.Model.Path.ToList();
					lst.Add(Model.Name);
					return lst;
				} else {
					return Model.Path;
				}
			};
			var preEle = RootCollection.GetNodeLine(new NodePath<string>(getPath()), AccountVM.CurrentDate)
				.Select(a => new { Key = a.Key, Value = a.Value as FinancialProduct })
				.LastOrDefault(a => a.Value != null && a.Key < AccountVM.CurrentDate)?.Value;
			if (preEle == null || (preEle.Amount == 0 && preEle.Quantity == 0)) {
					AccountVM.Elements.Remove(this);
					AccountVM.EdittingList.Add(AccountVM.CurrentDate);
			} else {
				MessageBox.Show("前回の記入項目から継続するポジションは削除できません。\n数量と評価額を「０」にした場合、次回の書き込み時に消滅または削除が可能となります。", "削除不可", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
		public virtual void Apply() {
			Model.Name = Name.Trim();
			Model.SetInvestmentValue((long)InvestmentValueView);
			Model.SetAmount((long)AmountView);
		}
		public bool IsCash => GetType() == typeof(CashEditVM);
		public bool IsStock => GetType() == typeof(StockEditVM);
		public bool IsProduct => GetType() == typeof(ProductEditVM);
		public virtual bool IsEmptyElement => false;
		public new FinancialValue Model => base.Model;
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();
		string _name = "";
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
		string _InvestmentValue = "";
		public virtual double InvestmentValueView => ExpParse.Try(_InvestmentValue);
		public virtual string InvestmentValue {
			get { return _InvestmentValue; }
			set {
				if (SetProperty(ref _InvestmentValue, value)) {
					OnPropertyChanged(nameof(InvestmentValueView));
					OnPropertyChanged(nameof(IsEmptyElement));
				}
			}
		}
		string _Amount;
		public double AmountView => ExpParse.Try(_Amount);
		public virtual string Amount {
			get { return _Amount; }
			set {
				if (SetProperty(ref _Amount, value)) {
					OnPropertyChanged(nameof(AmountView));
					OnPropertyChanged(nameof(IsEmptyElement));
					if (AccountVM.Elements.Contains(this)) AccountVM.ChangedTemporaryAmount();
				}
			}
		}

		public virtual bool IsTradeQuantityEditable => false;
		public virtual bool IsQuantityEditable => false;
		public virtual bool IsPerPriceEditable => false;
	}
	public class ProductEditVM : CashEditVM {
		public ProductEditVM(AccountEditVM ac, FinancialProduct fp) : base(ac, fp) {
			_TradeQuantity = fp.TradeQuantity.ToString();
			_CurrentPerPrice = fp.Quantity != 0 ? (fp.Amount / fp.Quantity).ToString("#.##") : "0";
			_Quantity = fp.Quantity.ToString();
		}
		public override void Apply() {
			base.Apply();
			Model.SetTradeQuantity((long)TradeQuantityView);
			Model.SetQuantity((long)QuantityView);
		}
		public ProductEditVM(AccountEditVM ac) : base(ac, new FinancialValue()) { }
		public override bool IsEmptyElement => AmountView == 0 && QuantityView == 0 && InvestmentValueView == 0 && TradeQuantityView == 0;
		public new FinancialProduct Model => base.Model as FinancialProduct;

		public override double InvestmentValueView {
			get {
				if (0 < TradeQuantityView)
					return Math.Abs(base.InvestmentValueView);
				else if (TradeQuantityView < 0)
					return Math.Abs(base.InvestmentValueView) * -1;
				else
					return base.InvestmentValueView;
			}
		}

		public override bool IsTradeQuantityEditable => true;
		string _TradeQuantity = "";
		public double TradeQuantityView => ExpParse.Try(_TradeQuantity);
		public virtual string TradeQuantity {
			get { return _TradeQuantity; }
			set {
				if (SetProperty(ref _TradeQuantity, value)) {
					OnPropertyChanged(nameof(TradeQuantityView));
					OnPropertyChanged(nameof(InvestmentValueView));
					OnPropertyChanged(nameof(IsEmptyElement));
					Quantity = (Model.Quantity + TradeQuantityView).ToString("#.##");
				}
			}
		}
		public override bool IsPerPriceEditable => true;
		string _CurrentPerPrice = "0";
		public double CurrentPerPriceView => ExpParse.Try(_CurrentPerPrice);
		public virtual string CurrentPerPrice {
			get { return _CurrentPerPrice; }
			set {
				if (SetProperty(ref _CurrentPerPrice, value)) {
					OnPropertyChanged(nameof(CurrentPerPriceView));
					Amount = (QuantityView * CurrentPerPriceView).ToString();
				}
			}
		}
		public override bool IsQuantityEditable => true;
		string _Quantity = "0";
		public double QuantityView => ExpParse.Try(_Quantity);
		public virtual string Quantity {
			get { return _Quantity; }
			set {
				if (SetProperty(ref _Quantity, value)) {
					OnPropertyChanged(nameof(QuantityView));
					OnPropertyChanged(nameof(IsEmptyElement));
					Amount = (QuantityView * CurrentPerPriceView).ToString();
				}
			}
		}
	}
	public class StockEditVM : ProductEditVM {
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
			return null;
		}
		async Task<string> setNameAndPrice(int r, DateTime d) {
			//this.AccountVM.SetStatusComment("コード: " + Code + " の銘柄情報を取得開始します");
			IEnumerable<StockInfo> siis = Enumerable.Empty<StockInfo>();
			try {
				siis = await Task.Run(() => Web.DownloadSource.AcqireStockInfo(d).Where(a => int.Parse(a.Symbol) == r).ToArray());
			} catch {
				return "通信状態を確認して再度実行してください。";
			} finally { }

			StockInfo si = null;
			if (!siis.Any()) {
				return "指定したコードの銘柄は存在しません。";
			} else {
				si = siis.OrderBy(a => a.Turnover).Last();
			}
			this.Name = si.Name;
			this.OnPropertyChanged(nameof(Name));
			if (si.Turnover != 0) {
				this.CurrentPerPrice = si.Close.ToString("#.##");
				return d.ToString("M月d日") + "における " + this.Name + " の終値を適用しました。";
			} else {
				return this.Name + "は出来高がないため終値を取得できませんでした。";
			}

		}
		public override void Apply() {
			base.Apply();
			Model.Code = ResultWithValue.Of<int>(int.TryParse, _Code).Value;
		}
		ViewModelCommand applySymbolCmd;
		public ViewModelCommand ApplySymbol
			=> applySymbolCmd = applySymbolCmd ?? new ViewModelCommand(applySymbol, canApplySymbol);

		void applySymbol() {
			ResultWithValue.Of<int>(int.TryParse, Code)
			   .TrueOrNot(async o => {
				   this.AccountVM.IsLoading = true;
				   var t = await setNameAndPrice(o, AccountVM.CurrentDate);
				   this.AccountVM.IsLoading = false;
				   AccountVM.SetStatusComment(t);
			   });
		}

		bool canApplySymbol()
			=> string.IsNullOrEmpty(codeValidate(this.Code));
	}
}
