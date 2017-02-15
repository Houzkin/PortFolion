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
		protected CommonNodeVM(CommonNode model) : base(model) {
			
			listener = new PropertyChangedWeakEventListener(model, ModelPropertyChanged);
			ReCalc();
		}
		IDisposable listener;
		protected override CommonNodeVM GenerateChild(CommonNode modelChildNode) {
			return CommonNodeVM.Create(modelChildNode);
			
			
		}
		bool isExpand;
		public bool IsExpand {
			get { return isExpand; }
			set { this.SetProperty(ref isExpand, value); }
		}
		public NodePath<string> Path => Model.Path;
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
			InvestmentTotal = CurrentPositionLine.Where(a => 0 < a.Value.InvestmentValue).Sum(a => a.Value.InvestmentValue);
			InvestmentReturnTotal = CurrentPositionLine.Where(a => 0 > a.Value.InvestmentValue).Sum(a => a.Value.InvestmentReturnValue) * -1;
			OnPropertyChanged(nameof(InvestmentTotal));
			OnPropertyChanged(nameof(InvestmentReturnTotal));

			OnPropertyChanged(nameof(ProfitLoss));
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
		public DateTime? CurrentDate => (Model.Root() as TotalRiskFundNode)?.CurrentDate;
		
		#region DataViewColumn
		public long InvestmentTotal { get; private set; }
		public long InvestmentReturnTotal { get; private set; }

		/// <summary>PL</summary>
		public long ProfitLoss {
			get {
				return (Model.Amount - InvestmentTotal + InvestmentReturnTotal);
			}
		}
		public virtual long UnrealizedProfitLoss {
			get {
				return Children.Sum(a => a.UnrealizedProfitLoss);
			}
		}
		public virtual double UnrealizedPLRatio
			=> Model.Amount != 0 ? UnrealizedProfitLoss / Model.Amount * 100 : 0;

		#endregion
		protected override void Dispose(bool disposing) {
			if (disposing) listener?.Dispose();
			base.Dispose(disposing);
		}
		public static CommonNodeVM Create(CommonNode node) {
			if (node == null) return null;
			var mcn = node.GetType();
			if (typeof(FinancialProduct).IsAssignableFrom(mcn)) {
				return new FinancialProductVM(node as FinancialProduct);
			}else {
				return new FinancialBacketVM(node);
			}
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
			
		}
		ViewModelCommand editCmd;
		public ICommand EditCommand {
			get {
				return editCmd;
			}
		}
	}
}
