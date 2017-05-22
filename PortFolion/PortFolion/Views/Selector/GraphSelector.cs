using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PortFolion.ViewModels;

namespace PortFolion.Views.Selector {
	
	public class ChartTemplateSelector : DataTemplateSelector {
		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			var element = container as FrameworkElement;
			var viewModel = item as GraphVmBase;
			if (element == null || viewModel == null) return null;
			var t = viewModel.GetType();
			// BalanceCashFlowChartTemplate,WeightChartTemplate,LogChartTemplate
			string templateName;
			if (t == typeof(BalanceSeries)) {
				templateName = "BalanceChartTemplate";
			}else if(t == typeof(IndexGraphVm)) {
				templateName = "IndexChartTemplate";
			}else {
				templateName = "NormalChartTemplate";
			}
			return element.FindResource(templateName) as DataTemplate;
		}
	}
	
}
