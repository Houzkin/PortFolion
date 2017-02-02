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
		/// <summary>平均取引コスト</summary>
		public double PerPriceAverage {
			get {
				return MaybeModelAs<FinancialProduct>().TrueOrNot(
					o => ((InvestmentTotal - InvestmentReturnTotal) / o.Quantity),
					x => 0);
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
