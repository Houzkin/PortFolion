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
using System.Text.RegularExpressions;

namespace PortFolion.ViewModels {
	public abstract class CommonEditVm : ReadOnlyBindableTreeNode<CommonNode, CommonEditVm> {
		protected CommonEditVm(CommonNode model) : base(model) { }
		protected override CommonEditVm GenerateChild(CommonNode modelChildNode) {
			switch (modelChildNode.GetNodeType()) {
			case IO.NodeType.Broker:
				return new BrokerEditVm(modelChildNode);
			case IO.NodeType.Account:
				return new AccountEditVm(modelChildNode);
			case IO.NodeType.Cash:
				return new CashEditVm(modelChildNode);
			case IO.NodeType.Stock:
				return new StockEditVm(modelChildNode);
			case IO.NodeType.OtherProduct:
				return new ProductEditVm(modelChildNode);
			}
			throw new NotImplementedException();
		}
		InteractionMessenger _im;
		public InteractionMessenger Messenger {
			get {
				if (this.IsRoot()) return _im = _im ?? new InteractionMessenger();
				else return this.Root().Messenger;
			}
			set { _im = value; }
		}
		public ObservableCollection<MenuItemVm> MenuList { get; } = new ObservableCollection<MenuItemVm>();

		HashSet<TotalRiskFundNode> _editSet;
		HashSet<TotalRiskFundNode> _EditSet => _editSet = _editSet ?? new HashSet<TotalRiskFundNode>();
		public void AddEditList(CommonNode node) {
			this.Root().AddEditList(new CommonNode[] { node });
		}
		public void AddEditList(IEnumerable<CommonNode> nodes) {
			if (!this.IsRoot())
				this.Root().AddEditList(nodes);
			else
				nodes.Select(a => a.Root())
					.OfType<TotalRiskFundNode>()
					.ForEach(a => _EditSet.Add(a));
		}
		public void Reset() {
			if (_editSet == null) return;
			RootCollection.ReRead(_EditSet);
			_EditSet.Clear();
		}
		
	}
	
	public class BasketEditVm : CommonEditVm{
		protected BasketEditVm(CommonNode node):base(node){ }
		bool _isExpand;
		public bool IsExpand{
			get{ return _isExpand; }
			set{ this.SetProperty(ref _isExpand, value); }
		}
		public void Add(CommonNode newNode){
			this.Model.AddChild(newNode);
		}
	}
	public class BrokerEditVm : BasketEditVm{ 
		public BrokerEditVm (CommonNode broker) : base(broker){ }
		
	}
	public class AccountEditVm : BasketEditVm{
		public AccountEditVm(CommonNode account):base(account){ }
	}
	public class CashEditVm : CommonEditVm{
		public CashEditVm(CommonNode model) : base(model){
			_InvestmenteValue = model.InvestmentValue.ToString();
			_Amount = model.Amount.ToString();
		}
		#region edit
		string _InvestmenteValue = "";
		public virtual string DisplayInvestmentValue{
			get{ return _InvestmenteValue; }
			set{
				if (SetProperty(ref _InvestmenteValue, value)) {
					Model.SetInvestmentValue((long)ExpParse.Try(value));
					OnPropertyChanged(nameof(IsDeletable));
				}
			}
		}
		string _Amount = "";
		public double AmountView => Model.Amount;
		public virtual string DisplayAmount{
			get{ return _Amount; }
			set{
				if(SetProperty(ref _Amount,value)){
					(Model as FinancialValue)?.SetAmount((long)ExpParse.Try(value));
					OnPropertyChanged(nameof(IsDeletable));
				}
			}
		}
		#endregion
		#region status
		public virtual bool IsDeletable => false;
		public virtual bool IsTradeQuantityEditable => false;
		public virtual bool IsQuantityEditable => false;
		public virtual bool IsPerPriceEditable => false;
		#endregion
	}
	public class ProductEditVm:CashEditVm{
		public ProductEditVm(CommonNode model):base(model){
		}
		protected new FinancialProduct Model => base.Model as FinancialProduct;
		#region TradeQuantity
		public override bool IsTradeQuantityEditable => true;
		string _TradeQuantity;
		public virtual string DisplayTradeQuantity{
			get{ return _TradeQuantity; }
			set{
				if(SetProperty(ref _TradeQuantity,value)){
					var v = (long)ExpParse.Try(value);
					Model.SetTradeQuantity(v);
					if (0 < v) Model.SetInvestmentValue(Math.Abs(Model.InvestmentValue));
					else if(v < 0) Model.SetInvestmentValue(Math.Abs(Model.InvestmentValue) * -1);
					Model.SetQuantity(Model.Quantity + v);
					OnPropertyChanged(nameof(IsDeletable));
				}
			}
		}
		#endregion
		#region Quantity
		public override bool IsQuantityEditable => true;
		string _Quantity;
		public virtual string DisplayQuantity{
			get{ return _Quantity; }
			set{
				if(SetProperty(ref _Quantity, value)){
					
					//Amount = 
					OnPropertyChanged(nameof(IsDeletable));
				}
			}
		}
		#endregion
		string _CurrentPerPrice;
		public override bool IsPerPriceEditable => true;
	}
	public class StockEditVm:ProductEditVm{
		public StockEditVm(CommonNode model):base(model){
		}
	}
}
