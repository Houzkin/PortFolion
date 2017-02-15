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
	public class AccountEditVM : ViewModel {
		AccountNode Model;
		FinancialValue cash;
		public AccountEditVM(AccountNode an) {
			Model = an;
			cash = Model.Children.FirstOrDefault(a => a.GetType() == typeof(FinancialValue)) as FinancialValue;
			if(cash == null) {
				cash = new FinancialValue();
				Model.AddChild(cash);
			}
		}
		public ObservableCollection<ProductVM> Products { get; private set; }
		public ObservableCollection<ProductVM> Stocks { get; private set; }

		public int AddNewCode { get; set; }
		public ViewModelCommand AddStockCommand;
		public ICommand AddStock { get; }

		public string AddNewProductName { get; set; }
		public ViewModelCommand AddProductCommand;
		public ICommand AddProduct { get; }

		ViewModelCommand exitCmd;
		public ICommand ExitCommand => exitCmd;
	}
	public class CashVM : ViewModel {
		public CashVM(FinancialValue fv) {

		}
		public long InvestmentValue { get; set; }
		public double Amount { get; set; }

	}
	public class ProductVM : CashVM {
		public ProductVM(FinancialProduct fp) : base(fp) { }
		public long Quantity { get; set; }
		public long TradeQuantity { get; set; }
		public double PerPrice { get; set; }

	}
	public class StockVM: ProductVM {
		public StockVM(StockValue sv) : base(sv) { }
		public int Code { get; set; }

	}
}
