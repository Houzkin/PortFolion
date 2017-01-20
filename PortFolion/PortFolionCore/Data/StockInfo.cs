using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.Core {
	public class StockInfo {
		/// <summary>ティッカーシンボルまはた銘柄コード</summary>
		public string Symbol { get; set; }
		/// <summary>銘柄名</summary>
		public string Name { get; set; }
		/// <summary>市場</summary>
		public string Market { get; set; }
		public double Open { get; set; }
		public double High { get; set; }
		public double Low { get; set; }
		public double Close { get; set; }
		/// <summary>出来高</summary>
		public ulong Turnover { get; set; }
		/// <summary>売買代金</summary>
		public double TradingValue { get; set; }
		public DateTime Date { get; set; }
	}
}
