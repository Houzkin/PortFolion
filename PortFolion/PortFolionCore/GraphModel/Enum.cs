using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.Core {
	public enum Period {
		Yearly,
		Quarterly,
		Monthly,
		Weekly,
	}
	public enum DividePattern {
		Tag,
		Location,
	}
	public enum CalculationUnit {
		Single,
		Total,
	}
	public enum Ratio {
		/// <summary>対数変化率</summary>
		Volatility,
		/// <summary>損益率</summary>
		Return,
		/// <summary>パフォーマンス</summary>
		Index,
	}
	public class TransitionParametor {
		public DividePattern Divide { get; set; }
		public Period TimePeriod { get; set; }
		public int TargetLevel { get; set; }
		public CalculationUnit InvestmentUnit { get; set; }
		public Ratio Ratio { get; set; }
	}
}
