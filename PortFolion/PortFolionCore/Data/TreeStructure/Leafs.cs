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
			obj.Node = NodeType.Cash;
			return obj;
		}
	}
	/// <summary>金融商品</summary>
	public class FinancialProduct : FinancialValue {
		internal FinancialProduct() { }
		internal FinancialProduct(CushionNode cushion) : base(cushion) {
			_quantity = cushion.Quantity;
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
		protected override CommonNode Clone(CommonNode node) {
			(node as FinancialProduct)._quantity = Quantity;
			return base.Clone(node);
		}
		public override CommonNode Clone() {
			return Clone(new FinancialProduct());
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Quantity = _quantity;
			obj.Node = NodeType.OtherProduct;
			return obj;
		}
	}
	public class StockValue : FinancialProduct {
		internal StockValue() { }
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
			obj.Node = NodeType.Stock;
			return obj;
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
			obj.Node = NodeType.Forex;
			return obj;
		}
	}
}
