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
		public long TradeQuantity { get; set; }
		public int Levarage { get; set; }
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
				cn = new AnonymousNode(cushion);
				break;
			}
			return cn;
		}
	}
	
}
