using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using System.Windows.Input;
using Houzkin.Architecture;
using PortFolion.Core;

namespace PortFolion.ViewModels {
	public abstract class CommonNodeVM : ReadOnlyBindableTreeNode<CommonNode, CommonNodeVM> {
		internal CommonNodeVM(CommonNode model) : base(model) {
		}

		protected override CommonNodeVM GenerateChild(CommonNode modelChildNode) {
			throw new NotImplementedException();
		}
		public bool IsExpand { get; set; }
		public abstract void Edit();
	}
}
