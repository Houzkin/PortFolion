using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PortFolion.ViewModels;

namespace PortFolion.Views.Selector {
	//public class GraphTemplateSelector : DataTemplateSelector {
	//	public override DataTemplate SelectTemplate(object item, DependencyObject container) {
	//		var element = container as FrameworkElement;
	//		var viewModel = item as GraphVmBase;
	//		if (element == null || viewModel == null) return null;
	//		var templateName = "DefaultGraphTemplate";// "TransitionTemplate";
	//		//var t = viewModel.GetType();
	//		//if (t == typeof(TransitionSeries)) {
	//		//	templateName = "TransitionTemplate";
	//		//} else if (t == typeof(TransitionStackCFSeries)) {
	//		//	templateName = "TransitionStackCFTemplate";
	//		//} else if (t == typeof(TransitionPLSeries)) {
	//		//	templateName = "TransitionPLTemplate";
	//		//}
	//		return element.FindResource(templateName) as DataTemplate;
	//		//return templateName == ""
	//		//	? null
	//		//	: element.FindResource(templateName) as DataTemplate;
	//	}
	//}
	public class ChartTemplateSelector : DataTemplateSelector {
		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			var element = container as FrameworkElement;
			var viewModel = item as GraphVmBase;
			if (element == null || viewModel == null) return null;
			// BalanceCashFlowChartTemplate,WeightChartTemplate,LogChartTemplate
			var templateName = "BalanceChartTemplate";// "NormalChartTemplate";
			return element.FindResource(templateName) as DataTemplate;
		}
	}
	
}
