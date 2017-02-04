using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using PortFolion.Core;
using Houzkin.Tree;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PortFolion.ViewModels {
	public class ListviewModel : ViewModel {
		RootCollection Model;
		public ListviewModel() {
			Model = RootCollection.Instance;
			Model.CollectionChanged += CollectionChanged;
			Root = RootCollection.Instance.LastOrDefault();
			if (Root != null) {
				CurrentDate = Root.CurrentDate;
				Path = Root.Path;
			}else {
				CurrentDate = DateTime.Today;
				Path = new NodePath<string>();
			}
		}

		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			var rt = Model.LastOrDefault(a => a.CurrentDate <= this.CurrentDate) ?? Model.LastOrDefault();
			if (Root == rt) return;
			if(rt == null) {
				Root = null;
				CurrentDate = DateTime.Today;
				Path = new NodePath<string>();
				Refresh();
				return;
			}else {
				Root = rt;
				SetCurrentDate(Root.CurrentDate);
			}
		}

		public DateTime CurrentDate { get; private set; }
		public TotalRiskFundNode Root { get; private set; }
		public NodePath<string> Path { get; private set; }
		public IEnumerable<CommonNodeVM> History
			=> RootCollection.GetNodeLine(Path).Select(a => new CommonNodeVM(a));
		
		public void SetCurrentDate(DateTime date) {
			date = date.Date;
			var c = RootCollection.Instance.LastOrDefault(a => date <= a.CurrentDate) ?? RootCollection.Instance.LastOrDefault();
			if (c == null) {
				if (Root == null) {
					CurrentDate = date;//notify
					RaisePropertyChanged(nameof(CurrentDate));
				}
				return;
			}
			Root = c;//notify
			CurrentDate = Root.CurrentDate;//notify
			
			if (Path.Any() && Root.Levelorder().Any(a => a.Path.SequenceEqual(Path))) {
				RaisePropertyChanged(nameof(Root));
				RaisePropertyChanged(nameof(CurrentDate));
				return;
			}
			var p = Root.Levelorder()
				.Select(a => a.Path.Zip(this.Path, (b, d) => new { b, d })
					.TakeWhile(e => e.b == e.d)
					.Select(f => f.b))
				.LastOrDefault() ?? Enumerable.Empty<string>();
			Path = new NodePath<string>(p);
			Refresh();
		}
		public void SetPath(NodePath<string> path) {
			if (path.SequenceEqual(Path)) return;
			Path = path;

			RaisePropertyChanged(nameof(this.Path));
			RaisePropertyChanged(nameof(this.History));
			RaisePropertyChanged(nameof(this.CurrentDate));
		}
		public void Refresh() {
			RaisePropertyChanged(nameof(this.Root));
			RaisePropertyChanged(nameof(this.Path));
			RaisePropertyChanged(nameof(this.History));
			RaisePropertyChanged(nameof(this.CurrentDate));
		}
	}
}
