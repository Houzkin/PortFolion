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
			OnPropertyChanged(nameof(PerPriceAverage));
			OnPropertyChanged(nameof(PerPrice));
		}
		/// <summary>平均取得コスト</summary>
		public double PerPriceAverage {
			get {
				var m = this.CurrentPositionLine.OfType<FinancialProduct>();
				if (!m.Any()) return 0;
				var nml = m.Zip(m.Skip(1), (a, b) => b.TradeQuantity == 0 || a.Quantity == 0 ? 1D : (b.Quantity - b.TradeQuantity) / a.Quantity)
					.Concat(new double[] { 1.0 })
					.Zip(m, (r, fp) => new { TQuanty = fp.TradeQuantity * r, TAmount = fp.InvestmentValue })
					.Where(a => a.TQuanty > 0)
					.Aggregate(
						new { TQuanty = 1D, TAmount = 1D },
						(a, b) => new { TQuanty = a.TQuanty + b.TQuanty, TAmount = a.TAmount + b.TAmount });
				return nml.TAmount / nml.TQuanty;
			}
		}
		/// <summary>現在単価</summary>
		public double PerPrice {
			get {
				return MaybeModelAs<FinancialProduct>().TrueOrNot(
					o => o.Amount / o.Quantity,
					x => 0);
			}
			//set {
			//	var fp = Model as FinancialProduct;
			//	if (fp == null) return;
			//	fp.SetAmount((long)(fp.Quantity * value));
			//}
		}
		//[ReflectReferenceValue]
		//public long Quantity {
		//	get {
		//		return MaybeModelAs<FinancialProduct>().TrueOrNot(
		//			o => o.Quantity,
		//			x => 0);
		//	}
		//	set {
		//		var fp = Model as FinancialProduct;
		//		if (fp == null || this.CurrentPerPrice == 0 || value == 0) return;
		//		if(!IsBlocking(nameof(Quantity)))
		//			fp.SetQuantity(value);
		//		fp.SetAmount((long)(value * CurrentPerPrice));
		//	}
		//}
	}
}
