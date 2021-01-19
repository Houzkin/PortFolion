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

		public virtual bool IsAdjustment { get; } = false;
	}
	public class AdjStockInfo : StockInfo {
		/// <summary>調整後終値</summary>
		public double AdjClose { get; set; }
		/// <summary>調整後終値/終値。現在の株式数から見て、最新の株式数は何倍(何分割)になっているか。</summary>
		public double Magnification { get => Close / AdjClose;　}
		public override bool IsAdjustment { get; } = true;
		public double AdjOpen { get => base.Open / Magnification; }
		public double AdjHigh { get => base.High / Magnification; }
		public double AdjLow { get => base.Low / Magnification; }
	}
}
