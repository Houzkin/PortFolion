using PortFolion.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PortFolion.Views.Selector {
	public class NodeTemplateSelector : DataTemplateSelector {
		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			var element = container as FrameworkElement;
			var viewModel = item as _CommonNodeVM;
			if (element == null || viewModel == null) return null;

			var templateName = "";
			var t = viewModel.GetType();
			if(t == typeof(_FinancialBasketVM)) {
				templateName = "NodeTemplate";
			}else {
				templateName = "LeafTemplate";
			}
			//switch (viewModel.Type) {
			//case "1": templateName = "Type1DataTemplate"; break;
			//case "2": templateName = "Type2DataTemplate"; break;
			//default: templateName = ""; break;
			//}

			return templateName == ""
				? null
				: element.FindResource(templateName) as DataTemplate;
		}
	}
}
