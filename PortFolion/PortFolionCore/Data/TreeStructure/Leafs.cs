using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Houzkin.Tree;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortFolion.IO;
using Houzkin;

namespace PortFolion.Core {

	public class FinancialValue : CommonNode {
		public FinancialValue() { }
		internal FinancialValue(CushionNode cushion) : base(cushion) {
			_amount = cushion.Amount;
		}
		protected override bool CanAddChild(CommonNode child) => false;

		long _amount;
		public void SetAmount(long amount) {
			if (_amount == amount) return;
			_amount = amount;
			RaisePropertyChanged(nameof(Amount));
		}
		public override long Amount {
			get { return _amount; }
		}
		public override bool HasPosition
			=> Amount != 0;
		public override bool HasTrading
			=> InvestmentValue != 0;

		protected override CommonNode Clone(CommonNode node) {
			(node as FinancialValue)._amount = Amount;
			return base.Clone(node);
		}
		public override CommonNode Clone() {
			return Clone(new FinancialValue());
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Amount = _amount;
			return obj;
		}
		public override NodeType GetNodeType() {
			return NodeType.Cash;
		}
	}
	/// <summary>金融商品</summary>
	public class FinancialProduct : FinancialValue {
		public FinancialProduct() { }
		internal FinancialProduct(CushionNode cushion) : base(cushion) {
			_quantity = cushion.Quantity;
			_tradeQuantity = cushion.TradeQuantity;
		}
		long _quantity;
		public void SetQuantity(long quantity) {
			if (_quantity == quantity) return;
			_quantity = quantity;
			RaisePropertyChanged(nameof(Quantity));
		}
		public long Quantity {
			get { return _quantity; }
		}
		public override bool HasPosition
			=> base.HasPosition || Quantity != 0;
		public override bool HasTrading
			=> base.HasTrading || TradeQuantity != 0;

		long _tradeQuantity;
		public void SetTradeQuantity(long tradeQuantity) {
			if (_tradeQuantity == tradeQuantity) return;
			_tradeQuantity = tradeQuantity;
			RaisePropertyChanged(nameof(TradeQuantity));
		}
		public long TradeQuantity => _tradeQuantity;
		protected override CommonNode Clone(CommonNode node) {
			var n = node as FinancialProduct;
			n._quantity = Quantity;
			return base.Clone(node);
		}
		public override CommonNode Clone() {
			return Clone(new FinancialProduct());
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Quantity = _quantity;
			obj.TradeQuantity = _tradeQuantity;
			return obj;
		}
		public override NodeType GetNodeType() {
			return NodeType.OtherProduct;
		}
	}
	public class StockValue : FinancialProduct {
		public StockValue() { }
		internal StockValue(CushionNode cushion) : base(cushion) {
			_code = ResultWithValue.Of<int>(int.TryParse, cushion.Code)
				.EitherWay(r => r);
		}
		int _code;
		public int Code {
			get { return _code; }
			set {
				if (_code == value) return;
				_code = value;
				RaisePropertyChanged();
			}
		}
		protected override CommonNode Clone(CommonNode node) {
			(node as StockValue)._code = Code;
			return base.Clone(node);
		}
		public override CommonNode Clone() {
			return Clone(new StockValue());
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Code = _code.ToString();
			return obj;
		}
		public override NodeType GetNodeType() {
			return NodeType.Stock;
		}
	}
	public class ForexValue : FinancialProduct {
		internal ForexValue() { }
		internal ForexValue(CushionNode cushion) : base(cushion) {
			_pair = cushion.Code;
		}
		string _pair;
		public string Pair {
			get { return _pair; }
			set {
				if (_pair == value) return;
				_pair = value;
				RaisePropertyChanged();
			}
		}
		protected override CommonNode Clone(CommonNode node) {
			(node as ForexValue)._pair = Pair;
			return base.Clone(node);
		}
		public override CommonNode Clone() {
			return Clone(new ForexValue());
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Code = Pair;
			return obj;
		}
		public override NodeType GetNodeType() {
			return NodeType.Forex;
		}
	}
}
