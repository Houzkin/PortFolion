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
	public class FinancialValueVM : CommonNodeVM {
		public FinancialValueVM(FinancialValue model) : base(model) { }
	}
	public class FinancialProductVM : FinancialBasketVM {
		public FinancialProductVM(FinancialProduct model) : base(model) {
		}
		protected override bool ModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
			if(base.ModelPropertyChanged(sender,e) 
				|| MaybeModelAs<FinancialProduct>().TrueOrNot(
					o=> e.PropertyName == nameof(o.Quantity) || e.PropertyName == nameof(o.TradeQuantity), 
					x => false)) {
				reculc();
				return true;
			}
			return false;
		}
		protected override void ReCalc() {
			base.ReCalc();
			reculc();
		}
		void reculc() {
			PerPrice = MaybeModelAs<FinancialProduct>().TrueOrNot(
					o => o.Amount / o.Quantity,
					x => 0D);
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
		public override long UnrealizedProfitLoss
			=> MaybeModelAs<FinancialProduct>().TrueOrNot(
				o => o.Amount - (long)(PerBuyPriceAverage * o.Quantity),
				x => 0);
		double _perBuyPriceAve;
		/// <summary>平均買付額</summary>
		public double PerBuyPriceAverage {
			get { return _perBuyPriceAve; }
			private set { SetProperty(ref _perBuyPriceAve, value); }
		}
		double _pp;
		/// <summary>現在単価</summary>
		public double PerPrice {
			get { return _pp; }
			set { SetProperty(ref _pp, value); }
		}
	}
}
		
