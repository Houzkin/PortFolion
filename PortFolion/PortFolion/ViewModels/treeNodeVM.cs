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

namespace PortFolion.ViewModels {
	public class CommonNodeVM : ReadOnlyBindableTreeNode<CommonNode, CommonNodeVM> {
		internal CommonNodeVM(CommonNode model) : base(model) {
			
			listener = new PropertyChangedWeakEventListener(model, ModelPropertyChanged);
			ReCalc();
		}
		IDisposable listener;
		protected override CommonNodeVM GenerateChild(CommonNode modelChildNode) {
			var mcn = modelChildNode.GetType();
			if (typeof(FinancialProduct).IsAssignableFrom(mcn)) {
				return new FinancialProductVM(modelChildNode as FinancialProduct);
			}else {
				return new FinancialBacketVM(modelChildNode);
			}
			
		}
		bool isExpand;
		public bool IsExpand {
			get { return isExpand; }
			set { this.SetProperty(ref isExpand, value); }
		}
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();
		/// <summary>再計算</summary>
		public void ReCalcurate() {
			foreach (var n in this.Root().Levelorder().Reverse()) n.ReCalc();
		}
		protected virtual void ModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(Model.Amount) 
				|| e.PropertyName == nameof(Model.InvestmentValue) 
				|| e.PropertyName == nameof(Model.InvestmentReturnValue)) {
				ReCalc();
			}
		}
		/// <summary>再計算内容</summary>
		protected virtual void ReCalc() {
			_currentPositionLine = null;
			InvestmentTotal = CurrentPositionLine.Sum(a => a.Value.InvestmentValue);
			InvestmentReturnTotal = CurrentPositionLine.Sum(a => a.Value.InvestmentReturnValue);
			OnPropertyChanged(nameof(InvestmentTotal));
			OnPropertyChanged(nameof(InvestmentReturnTotal));

			OnPropertyChanged(nameof(UnrealizedProfitLoss));
		}

		Dictionary<DateTime, CommonNode> _currentPositionLine;
		protected Dictionary<DateTime,CommonNode> CurrentPositionLine {
			get {
				if (_currentPositionLine != null) return _currentPositionLine;
				var d = (Model.Root() as TotalRiskFundNode).CurrentDate;
				_currentPositionLine = RootCollection
					.GetNodeLine(Model.Path, d)
					.Values
					.Select(value => new { (value.Root() as TotalRiskFundNode).CurrentDate, value })
					.Where(a => a.CurrentDate <= d)
					.ToDictionary(a => a.CurrentDate, a => a.value);
				return _currentPositionLine;
			}
		}
		
		#region DataViewColumn
		public long InvestmentTotal { get; private set; }
		public long InvestmentReturnTotal { get; private set; }

		/// <summary>含み</summary>
		public long UnrealizedProfitLoss {
			get {
				return (Model.Amount - InvestmentTotal + InvestmentReturnTotal);
			}
		}
		#endregion
		protected override void Dispose(bool disposing) {
			if (disposing) listener?.Dispose();
			base.Dispose(disposing);
		}
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
	public class FinancialBacketVM : CommonNodeVM {
		public FinancialBacketVM(CommonNode model) : base(model){
			init();
		}
		private void init() {
			var type = Model.GetType();
			if(type == typeof(TotalRiskFundNode)) {

			}else if(type == typeof(BrokerNode)) {

			}else if(type == typeof(AccountNode)) {

			}else if(typeof(FinancialValue).IsAssignableFrom(type)) {

			}
		}
	}
	//public class TotalRiskFundNodeVM : CommonNodeVM {
	//	public TotalRiskFundNodeVM(TotalRiskFundNode model) : base(model) {
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "ブローカーを追加" });
	//	}
	//}
	//public class BrokerNodeVM : CommonNodeVM {
	//	public BrokerNodeVM(AccountNode model) : base(model) {
	//		var addItem = new MenuItemVm() { Header = "追加" };
	//		addItem.Children.Add(new MenuItemVm(()=> { }) { Header = "一般" });
	//		addItem.Children.Add(new MenuItemVm(()=> { }) { Header = "信用" });
	//		addItem.Children.Add(new MenuItemVm(()=> { }) { Header = "為替" });
	//		MenuList.Add(addItem);
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "ブローカー名の変更" });
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "ブローカーを除外" });
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "タグの編集" });
	//	}
	//}
	//public class AccountNodeVM : CommonNodeVM {
	//	public AccountNodeVM(AccountNode model) : base(model) {
	//		MenuItemVm addItem;
	//		switch (model.Account) {
	//		case AccountClass.General:
	//			addItem = new MenuItemVm(() => { }) { Header = "新規買付" };
	//			break;
	//		case AccountClass.Credit:
	//			addItem = new MenuItemVm(() => { }) { Header = "新規建玉" };
	//			break;
	//		case AccountClass.FX:
	//			addItem = new MenuItemVm(() => { }) { Header = "新規ポジション" };
	//			break;
	//		default:
	//			addItem = new MenuItemVm(() => { });
	//			break;
	//		}
	//		MenuList.Add(addItem);
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "InvestOrRetrun" });
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "アカウント名の変更" });
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "アカウントを除外" });
	//		MenuList.Add(new MenuItemVm(()=> { }) { Header = "タグの編集" });
	//	}
		
	//}
}
