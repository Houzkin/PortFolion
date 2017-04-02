using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion._Core {
	public enum Period {
		Yearly,
		Quarterly,
		Monthly,
		Weekly,
	}
	public enum _DividePattern {
		Tag,
		Location,
	}
	public enum _CalculationUnit {
		Single,
		Total,
	}
	public enum _Ratio {
		/// <summary>対数変化率</summary>
		Volatility,
		/// <summary>損益率</summary>
		Return,
		/// <summary>パフォーマンス</summary>
		Index,
	}
	public class _TransitionParametor {
		public _DividePattern Divide { get; set; }
		public Period TimePeriod { get; set; }
		public int TargetLevel { get; set; }
		public _CalculationUnit InvestmentUnit { get; set; }
		public _Ratio Ratio { get; set; }
	}
}
