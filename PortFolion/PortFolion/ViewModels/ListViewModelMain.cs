using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using PortFolion.Core;
using Houzkin.Tree;
using System.Collections.ObjectModel;

namespace PortFolion.ViewModels {
	public class ListviewModel : ViewModel {
		public ListviewModel() {
			Root = RootCollection.Instance.LastOrDefault();
			if (Root != null) {
				CurrentDate = Root.CurrentDate;
				Path = Root.Path;
				History = new ObservableCollection<CommonNodeVM>(RootCollection.GetNodeLine(Path).Select(a => new CommonNodeVM(a)));
			}else {
				CurrentDate = DateTime.Today;
				Path = new NodePath<string>();
				History = new ObservableCollection<CommonNodeVM>();
			}
		}
		public DateTime CurrentDate { get; private set; }
		public TotalRiskFundNode Root { get; private set; }
		public NodePath<string> Path { get; private set; }
		public ObservableCollection<CommonNodeVM> History { get; private set; }
		
		public void SetCurrentDate(DateTime date) {
			date = date.Date;
			var c = RootCollection.Instance.LastOrDefault(a => date <= a.CurrentDate) ?? RootCollection.Instance.LastOrDefault();
			if (c == null) {
				if (Root == null) {
					CurrentDate = date;//notify
				}
				return;
			}
			Root = c;//notify
			CurrentDate = Root.CurrentDate;//notify
			if (!Root.Levelorder().Any(a => a.Path.SequenceEqual(Path))) {
				Root.Levelorder().Select(a => a.Path.Zip(this.Path, (b, d) => new { b, d }).TakeWhile(e => e.b == e.d));
			}

		}
		
		
	}
}
