using PortFolion.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PortFolion.Views {
	/// <summary>
	/// ListTab.xaml の相互作用ロジック
	/// </summary>
	public partial class ListTab : UserControl {
		public ListTab() {
			InitializeComponent();
			this.DataContext = new ListviewModel();
			
		}

		private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
			var vm = this.DataContext as ListviewModel;
			var d = e.Parameter as VmCoreBase;
			if (d != null && vm != null)
				vm.CurrentDate = d.CurrentDate;
		}
		
	}
}
