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

namespace PortFolion.ViewModels {
	public abstract class CommonNodeVM : ReadOnlyBindableTreeNode<CommonNode, CommonNodeVM> {
		internal CommonNodeVM(CommonNode model) : base(model) {
		}

		protected override CommonNodeVM GenerateChild(CommonNode modelChildNode) {
			var mcn = modelChildNode.GetType();
			if(mcn == typeof(TotalRiskFundNode)) {

			}else if(mcn == typeof(BrokerNode)){

			}else if(mcn == typeof(AccountNode)) {

			}else if(mcn == typeof(StockValue)) {
			}else if(mcn == typeof(ForexValue)) {

			}else if(mcn == typeof(FinancialProduct)) {

			}else if(mcn == typeof(FinancialValue)) {

			}
			return null;
		}
		bool isExpand;
		public bool IsExpand {
			get { return isExpand; }
			set { this.SetProperty(ref isExpand, value); }
		}
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();
		public void RaiseReftesh() {
			foreach (var n in this.Upstream()) n.Refresh();
		}
		protected virtual void Refresh() {

		}
		Dictionary<DateTime, CommonNode> _nodeLine;
		Dictionary<DateTime,CommonNode> nodeLine {
			get {
				if (_nodeLine != null) return _nodeLine;
				var d = (Model.Root() as TotalRiskFundNode).CurrentDate;
				_nodeLine = RootCollection
					.GetNodeLine(Model.Path, d)
					.Select(value => new { (value.Root() as TotalRiskFundNode).CurrentDate, value })
					.Where(a => a.CurrentDate <= d)
					.ToDictionary(a => a.CurrentDate, a => a.value);
				return _nodeLine;
			}
		}
		#region DataViewColumn
		//amount
		//quantity
		//investment
		//investmentReturn
		//investmentTotal
		//investmentReturnTotal
		//profitLoss
		//profitLossRatio
		//currentPrice
		public long InvestmentTotal {
			get { 
				return nodeLine
					.Sum(a => a.Value.InvestmentValue);
			}
		}
		public long InvestmentReturnTotal {
			get {
				return nodeLine.Sum(a => a.Value.InvestmentReturnValue);
			}
		}
		#endregion

	}
	public class MenuItemVm {
		public string Header { get; set; }
		public MenuItemVm() : this(() => { }) { }
		public MenuItemVm(ICommand command) {
			menuCommand = command;
		}
		public MenuItemVm(Action execute) : this(execute,()=>true) { }
		public MenuItemVm(Action execute,Func<bool> canExecute) {
			menuCommand = new ViewModelCommand(execute, canExecute);
		}
		ICommand menuCommand;
		public ICommand MenuCommand => menuCommand;
		ObservableCollection<MenuItemVm> children;

		public ObservableCollection<MenuItemVm> Children 
			=> children = children ?? new ObservableCollection<MenuItemVm>();
		
	}
	public class TotalRiskFundNodeVM : CommonNodeVM {
		public TotalRiskFundNodeVM(TotalRiskFundNode model) : base(model) {
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "ブローカーを追加" });
		}
	}
	public class BrokerNodeVM : CommonNodeVM {
		public BrokerNodeVM(AccountNode model) : base(model) {
			var addItem = new MenuItemVm() { Header = "追加" };
			addItem.Children.Add(new MenuItemVm(()=> { }) { Header = "一般" });
			addItem.Children.Add(new MenuItemVm(()=> { }) { Header = "信用" });
			addItem.Children.Add(new MenuItemVm(()=> { }) { Header = "為替" });
			MenuList.Add(addItem);
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "ブローカー名の変更" });
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "ブローカーを除外" });
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "タグの編集" });
		}
	}
	public class AccountNodeVM : CommonNodeVM {
		public AccountNodeVM(AccountNode model) : base(model) {
			MenuItemVm addItem;
			switch (model.Account) {
			case AccountClass.General:
				addItem = new MenuItemVm(() => { }) { Header = "新規買付" };
				break;
			case AccountClass.Credit:
				addItem = new MenuItemVm(() => { }) { Header = "新規建玉" };
				break;
			case AccountClass.FX:
				addItem = new MenuItemVm(() => { }) { Header = "新規ポジション" };
				break;
			default:
				addItem = new MenuItemVm(() => { });
				break;
			}
			MenuList.Add(addItem);
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "InvestOrRetrun" });
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "アカウント名の変更" });
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "アカウントを除外" });
			MenuList.Add(new MenuItemVm(()=> { }) { Header = "タグの編集" });
		}
		
	}
}
