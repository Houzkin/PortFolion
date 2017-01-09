using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using Houzkin.Tree.Serialization;
using PortFolion.Core;
using Houzkin;

namespace PortFolion.IO {
	public enum NodeType {
		Unknown,
		Total,
		Broker,
		Account,
		Cash,
		Stock,
		Forex,
		OtherProduct,
	}
	public class CushionNode : TreeNode<CushionNode> {
		/// <summary>デシリアライズ時に呼び出されるコンストラクタ</summary>
		public CushionNode() { }
		
		public NodeType Node { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }
		public long Amount { get; set; }
		public long Quantity { get; set; }
		public string Tag { get; set; }
		public long InvestmentValue { get; set; }
		public long InvestmentReturnValue { get; set; }
		public AccountClass Account { get; set; }
		public DateTime Date { get; set; }

		public CommonNode ToInstance() {
			return Convert(this);
		}
		static CommonNode Convert(CushionNode cushion) {
			CommonNode cn;
			switch (cushion.Node) {
			case NodeType.Total:
				cn = new TotalRiskFundNode(cushion);
				break;
			case NodeType.Broker:
				cn = new BrokerNode(cushion);
				break;
			case NodeType.Account:
				cn = new AccountNode(cushion);
				break;
			case NodeType.Cash:
				cn = new FinancialValue(cushion);
				break;
			case NodeType.Stock:
				cn = new StockValue(cushion);
				break;
			case NodeType.Forex:
				cn = new ForexValue(cushion);
				break;
			case NodeType.OtherProduct:
				cn = new FinancialProduct(cushion);
				break;
			default:
				cn = null;
				break;
			}
			return cn;
		}
	}
	public class CushionConverter {

		//public static CushionNode ToSeri(CommonNode node) {
		//	foreach(var f in Convs) {
		//		var n = f(node);
		//		if (n != null) return n;
		//	}
		//	throw new InvalidCastException();
		//}
		//public static CommonNode ToDeseri(CushionNode node) {
		//	foreach(var f in Decs) {
		//		var n = f(node);
		//		if (n != null) return n;
		//	}
		//	throw new InvalidCastException();
		//}
		//static List<Func<CommonNode, CushionNode>> Convs { get; } 
		//	= new List<Func<CommonNode,CushionNode>>() {
		//	ConvTotalRiskFundNode,
		//};
		//static List<Func<CushionNode, CommonNode>> Decs { get; } 
		//	= new List<Func<CushionNode, CommonNode>>() {
		//	DecTotalRiskFundNode,
		//};


		//static ResultWithValue<T> If<T>(T value,Predicate<T> pred) where T : class {
		//	if (pred(value)) return new ResultWithValue<T>(value);
		//	else return new ResultWithValue<T>();
		//}
		//static ResultWithValue<T> IfNull<T>(T value) where T : class {
		//	return If(value, a => a == null);
		//}

		//#region TotalRiskFund
		//static CushionNode ConvTotalRiskFundNode(CommonNode node) {
		//	return IfNull(node as TotalRiskFundNode).TrueOrNot(
		//		o => null,
		//		x => new CushionNode() {
		//			Node = NodeType.Total,
		//			Name = x.Name,
		//			Date = x.CurrentDate,
		//		});
		//}
		//static CommonNode DecTotalRiskFundNode(CushionNode node) {
		//	return If(node, a => a.Node == NodeType.Total).TrueOrNot(
		//		o => new TotalRiskFundNode() {
		//			Name = o.Name,
		//			CurrentDate = o.Date,
		//		},
		//		x => null);
		//}
		//#endregion

		//#region Broker
		//static CushionNode ConvBrokerNode(CommonNode node) {
		//	return IfNull(node as BrokerNode).TrueOrNot(
		//		o => null,
		//		x => new CushionNode() {
		//			Node = NodeType.Broker,
		//			Name = x.Name,
		//			Tag = x.Tag.TagName
		//		});
		//}
		//static CommonNode DecBrokerNode(CushionNode node) {
		//	return If(node, a => a.Node == NodeType.Broker).TrueOrNot(
		//		o => new BrokerNode() {
		//			Name = o.Name,
		//			Tag = TagInfo.GetOrCreate(o.Tag),
		//		},
		//		x => null);
		//}
		//#endregion
		//#region Account
		//static CushionNode ConvAccountNode(CommonNode node) {
		//	return IfNull(node as AccountNode).TrueOrNot(
		//		o => null,
		//		x => new CushionNode() {
		//			Node = NodeType.Account,
		//			Name = x.Name,
		//			Tag = x.Tag.TagName,
		//			Account = x.Account,
		//			InvestmentValue = x.InvestmentValue,
		//			InvestmentReturnValue = x.InvestmentReturnValue,
		//		});
		//}
		//static CommonNode DecAccountNode(CushionNode node) {
		//	return If(node, a => a.Node == NodeType.Account).TrueOrNot(
		//		o => {
		//			var ac = new AccountNode() {
		//				Name = o.Name,
		//				Tag = TagInfo.GetOrCreate(o.Tag),
		//				Account = o.Account,
		//			};
		//			ac.SetInvestmentValue(o.InvestmentValue);
		//			ac.SetInvestmentReturnValue(o.InvestmentReturnValue);
		//			return ac;
		//		},
		//		x => null);
		//}
		//#endregion

		//#region Cash
		//static CushionNode ConvCashNode(CommonNode node) {
		//	return IfNull(node as CashValue).TrueOrNot(
		//		o => null,
		//		x => new CushionNode() {
		//			Node = NodeType.Cash,
		//			Name = x.Name,
		//			Tag = x.Tag.TagName,
		//			Amount = x.Amount,
		//			InvestmentValue = x.InvestmentValue,
		//			InvestmentReturnValue = x.InvestmentReturnValue,
		//		});
		//}
		//static CommonNode DecCashNode(CushionNode node) {
		//	return If(node, a => a.Node == NodeType.Cash).TrueOrNot(
		//		o => {
		//			var cv = new CashValue() {
		//				Name = o.Name,
		//				Tag = TagInfo.GetOrCreate(o.Tag),
		//			};
		//			cv.SetInvestmentValue(o.InvestmentValue);
		//			cv.SetInvestmentReturnValue(o.InvestmentReturnValue);
		//			return cv;
		//		},
		//		x => null);
		//}
		//#endregion

		//#region Stock
		//static CushionNode ConvStockNode(CommonNode node) {

		//}
		//static CommonNode DecStockNode(CushionNode node) {

		//}
		//#endregion

		//#region OtherProduct
		//static CushionNode ConvOtherProduct(CommonNode node) {
		//	return IfNull(node as OtherValue).TrueOrNot(
		//		o => null,
		//		x => new CushionNode() {
		//			Node = NodeType.OtherProduct,
		//			Name = x.Name,
		//			Tag = x.Tag.TagName,
		//			Amount = x.Amount,
		//			InvestmentValue = x.InvestmentValue,
		//			InvestmentReturnValue = x.InvestmentReturnValue,
		//			Quantity = x.Quantity,
		//		});
		//}
		//static CommonNode DecOtherProduct(CushionNode node) {
		//	return If(node, a => a.Node == NodeType.OtherProduct).TrueOrNot(
		//		o => {
		//			var ov = new OtherValue() {
		//				Name = o.Name,
		//				Tag = TagInfo.GetOrCreate(o.Tag),
		//			};
		//			ov.SetInvestmentValue(o.InvestmentValue);
		//			ov.SetInvestmentReturnValue(o.InvestmentReturnValue);
		//			ov.SetQuantity(o.Quantity);
		//			return ov;
		//		},
		//		x => null);
		//}
		//#endregion


	}
}
