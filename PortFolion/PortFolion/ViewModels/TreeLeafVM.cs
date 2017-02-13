using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortFolion.Core;
using System.Windows.Input;
using Houzkin.Architecture;
using System.ComponentModel;
using Livet.EventListeners.WeakEvents;

namespace PortFolion.ViewModels {

	public class FinancialProductVM : CommonNodeVM {
		public FinancialProductVM(FinancialProduct model) : base(model) {
		}
		protected override void ModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.ModelPropertyChanged(sender, e);
			MaybeModelAs<FinancialProduct>().TrueOrNot(
				o => {
					if (e.PropertyName == nameof(o.Quantity) || e.PropertyName == nameof(o.Amount)) ReCalc();
				});
		}
		protected override void ReCalc() {
			base.ReCalc();
			OnPropertyChanged(nameof(PerPrice));
			var m = this.CurrentPositionLine.OfType<FinancialProduct>();
			if (m.Any()) {
				var nml = m.Zip(m.Skip(1), (a, b) => a.Quantity == 0 ? 1D : (b.Quantity - b.TradeQuantity) / a.Quantity)
						.Concat(new double[] { 1.0 })
						.Reverse()
						.Scan(1D, (a, b) => a * b)
						.Reverse()
						.Zip(m, (r, fp) => new { TQuanty = fp.TradeQuantity * r, TAmount = fp.InvestmentValue })
						.Where(a => a.TQuanty > 0)
						.Aggregate(
							new { TQuanty = 1D, TAmount = 1D },
							(a, b) => new { TQuanty = a.TQuanty + b.TQuanty, TAmount = a.TAmount + b.TAmount });
				PerBuyPriceAverage = nml.TAmount / nml.TQuanty;
			} else {
				PerBuyPriceAverage = 0;
			}
			OnPropertyChanged(nameof(UnrealizedProfitLoss));
			OnPropertyChanged(nameof(UnrealizedPLRatio));
		}
		double _perBuyPriceAve;
		/// <summary>平均買付額</summary>
		public double PerBuyPriceAverage {
			get { return _perBuyPriceAve; }
			private set { SetProperty(ref _perBuyPriceAve, value); }
		}
		/// <summary>現在単価</summary>
		public double PerPrice {
			get {
				return MaybeModelAs<FinancialProduct>().TrueOrNot(
					o => o.Amount / o.Quantity,
					x => 0);
			}
		}
		/// <summary>含み</summary>
		public override long UnrealizedProfitLoss {
			get {
				return MaybeModelAs<FinancialProduct>().TrueOrNot(
					o => o.Amount - (long)(PerBuyPriceAverage * o.Quantity),
					x => 0);
			}
		}
		//public override double UnrealizedPLRatio
		//	=> Model.Amount != 0 ? UnrealizedProfitLoss / Model.Amount * 100 : 0;
		//	get {
		//		return MaybeModelAs<FinancialProduct>().TrueOrNot(
		//			o => o.Amount != 0 ? UnrealizedProfitLoss / o.Amount * 100 : 0,
		//			x => 0);
		//	}
		//}
	}
}
		
