using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PortFolion.ViewModels;

namespace PortFolion.Views.Selector {
	public class GraphTemplateSelector : DataTemplateSelector {
		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			var element = container as FrameworkElement;
			var viewModel = item as object;
			if (element == null || viewModel == null) return null;

			var templateName = "";
			var t = viewModel.GetType();
			if (t == typeof(TransitionSeries)) {
				templateName = "TransitionTemplate";
			} else if (t == typeof(TransitionStackCFSeries)) {
				templateName = "TransitionStackCFTemplate";
			} else if (t == typeof(TransitionPLSeries)) {
				templateName = "TransitionPLTemplate";
			}// else if (t == typeof()) {

			//} else if (t == typeof()) {

			//} else if (t == typeof()) {

			//}
			return templateName == ""
				? null
				: element.FindResource(templateName) as DataTemplate;
		}
	}
}
