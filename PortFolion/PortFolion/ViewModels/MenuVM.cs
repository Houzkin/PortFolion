using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using System.Windows.Input;

namespace PortFolion.ViewModels {
	public class MenuViewModel : ObservableTreeNode<MenuViewModel> {
		public virtual ICommand Action { get; }
		public virtual string Header { get; set; }
		public static MenuViewModel Create() {
			var r = new MenuViewModel();

			return r
			.AddChild(new MenuViewModel() { Header = "編集" }
				.AddChild(new MenuViewModel() { Header = "test" }))
			.AddChild(new MenuViewModel() { Header = "設定" }
				.AddChild(new MenuViewModel { Header = "設定test" }))
			.AddChild(new MenuViewModel() { Header = "ライセンス" });
			//return r;
			//return new MenuViewModel()
			//	.AddChild(new MenuViewModel { Header = "Sample1" }
			//		.AddChild(new MenuViewModel { Header = "Sample01" })
			//		.AddChild(new MenuViewModel { Header = "Sample02" }))
			//	.AddChild(new MenuViewModel { Header = "Sample2" }
			//		.AddChild(new MenuViewModel { Header = "Sample001" }));
		}
	}
}
