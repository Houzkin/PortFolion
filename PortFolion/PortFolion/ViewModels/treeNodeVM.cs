﻿using System;
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
			
			listener = new PropertyChangedWeakEventListener(model, new PropertyChangedEventHandler((o,e)=> { ModelPropertyChanged(o, e); }));
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
			foreach (var n in this.Levelorder().Reverse()) n.ReCalc();
			//foreach (var n in this.Root().Levelorder().Reverse()) n.ReCalc();
		}
		protected virtual bool ModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(Model.InvestmentValue)) {
				reculcHistories();
				return true;
			}
			return false;
		}
		void reculcHistories() {
			_currentPositionLine = null;
			InvestmentTotal = CurrentPositionLine.Where(a => 0 < a.Value.InvestmentValue).Sum(a => a.Value.InvestmentValue);
			InvestmentReturnTotal = CurrentPositionLine.Where(a => 0 > a.Value.InvestmentValue).Sum(a => a.Value.InvestmentValue) * -1;
		}
		/// <summary>再計算内容</summary>
		protected virtual void ReCalc() {
			reculcHistories();
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
		long _invTotal;
		public virtual long InvestmentTotal {
			get { return _invTotal; }
			set {
				if (_invTotal == value) return;
				_invTotal = value;
				OnPropertyChanged();
			}
		}
		long _invReturnTotal;
		public virtual long InvestmentReturnTotal {
			get { return _invReturnTotal; }
			set {
				if (_invReturnTotal == value) return;
				_invReturnTotal = value;
				OnPropertyChanged();
			}
		}

		#endregion
		protected override void Dispose(bool disposing) {
			if (disposing) listener?.Dispose();
			base.Dispose(disposing);
		}
		public static CommonNodeVM Create(CommonNode node) {
			if (node == null) return null;
			var mcn = node.GetType();
			if (typeof(FinancialProduct)== mcn) {
				return new FinancialProductVM(node as FinancialProduct);
			}else if (typeof(FinancialValue)== mcn) {
				return new FinancialValueVM(node as FinancialValue);
			}else {
				return new FinancialBasketVM(node);
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
	public class FinancialBasketVM : CommonNodeVM {
		public FinancialBasketVM(CommonNode model) : base(model){
			var ty = model.GetType();
			if(ty == typeof(AccountNode)) {
				var vc = new ViewModelCommand(() => {
					var vm = new AccountEditVM(model as AccountNode);
					var w = new Views.AccountEditWindow(vm);
					w.ShowDialog();
				});
				MenuList.Add(new MenuItemVm(vc) { Header = "編集" });
			}

			var vmc = new ViewModelCommand(() => {
				var vm = new NodeNameEditerVM(model.Parent, model);
				// window.ShowDialog();
			}, () => model.Parent != null);
			MenuList.Add(new MenuItemVm(vmc) { Header = "名前の変更" });
			
		}
		protected override void ReCalc() {
			base.ReCalc();
			reculc();
		}
		protected override bool ModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
			if(base.ModelPropertyChanged(sender,e) 
				|| e.PropertyName == nameof(Model.Amount)) {
				reculc();
				return true;
			}
			return false;
		}
		void reculc() {
			this.ProfitLoss = Model.Amount - InvestmentTotal - InvestmentReturnTotal;
			this.UnrealizedProfitLoss = Children.OfType<FinancialBasketVM>().Sum(a => a.UnrealizedProfitLoss);
			//this.UnrealizedPLRatio = Model.Amount != 0 ? UnrealizedProfitLoss / Model.Amount * 100 : 0;
			OnPropertyChanged(nameof(UnrealizedPLRatio));
		}
		long _pl;
		/// <summary>PL</summary>
		public long ProfitLoss {
			get { return _pl; }
			set { SetProperty(ref _pl, value); }
		}
		long _upl;
		public virtual long UnrealizedProfitLoss {
			get { return _upl; }
			set { SetProperty(ref _upl, value); }
		}
		public double UnrealizedPLRatio
			=> Model.Amount != 0 ? UnrealizedProfitLoss / Model.Amount * 100 : 0;
	}
}
