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
			case IO.NodeType.Total:
				return new BasketEditVm(modelChildNode);
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
		public BasketEditVm(CommonNode node):base(node){ }
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
		}
		#region edit
		public virtual double InvestmentValue{
			get{ return Model.InvestmentValue; }
			set{
				Model.SetInvestmentValue((long)value);
				OnPropertyChanged(nameof(IsEmptyElement));
			}
		}
		public virtual double Amount{
			get{ return Model.Amount; }
			set{
				(Model as FinancialValue)?.SetAmount((long)value);
				OnPropertyChanged(nameof(IsEmptyElement));
			}
		}
		#endregion
		#region status
		public virtual bool IsEmptyElement => false;
		public virtual bool IsTradeQuantityEditable => false;
		public virtual bool IsQuantityEditable => false;
		public virtual bool IsPerPriceEditable => false;
		#endregion
	}
	public class ProductEditVm:CashEditVm{
		public ProductEditVm(CommonNode model):base(model){
			this._CurrentPerPrice = Model.Amount / Model.Quantity;
		}
		protected new FinancialProduct Model => base.Model as FinancialProduct;
		public override bool IsEmptyElement =>
			Amount == 0 && Quantity == 0 && InvestmentValue == 0 && TradeQuantity == 0;
		#region InvestmentValue
		public override double InvestmentValue {
			get { return base.InvestmentValue; }
			set {
				if (0 < value)
					Model.SetTradeQuantity(Math.Abs(Model.TradeQuantity));
				else if (value < 0)
					Model.SetTradeQuantity(Math.Abs(Model.TradeQuantity) * -1);
				base.InvestmentValue = value;
			}
		}
		#endregion
		#region TradeQuantity
		public override bool IsTradeQuantityEditable => true;
		public virtual double TradeQuantity{
			get{ return Model.TradeQuantity; }
			set{
				Model.SetTradeQuantity((long)value);
				if (0 < value) Model.SetInvestmentValue(Math.Abs(Model.InvestmentValue));
				else if (value < 0) Model.SetInvestmentValue(Math.Abs(Model.InvestmentValue) * -1);
				OnPropertyChanged(nameof(IsEmptyElement));
			}
		}
		#endregion
		#region Quantity
		public override bool IsQuantityEditable => true;
		string _Quantity;
		public virtual double Quantity{
			get{ return Model.Quantity; }
			set{
				Model.SetQuantity((long)value);
				OnPropertyChanged(nameof(IsEmptyElement));
			}
		}
		#endregion
		#region CurrentPerPrice
		public override bool IsPerPriceEditable => true;
		double _CurrentPerPrice;
		public double CurrentPerPrice{
			get{ return _CurrentPerPrice; }
			set{
				if (SetProperty(ref _CurrentPerPrice, value)) {
					Model.SetAmount((long)(Quantity * value));
				}
			}
		}
		#endregion
	}
	public class StockEditVm:ProductEditVm{
		public StockEditVm(CommonNode model):base(model){ 
		}
		public new StockValue Model => base.Model as StockValue;
		public int Code{
			get{ return Model.Code; }
			set{
				Model.Code = value;
				this.ValidateProperty(value,Validate);
			}
		}
		protected virtual string Validate(int code){
			if (code.ToString().Count() != 4) return "4桁";
			return null;
		}
	}
}
