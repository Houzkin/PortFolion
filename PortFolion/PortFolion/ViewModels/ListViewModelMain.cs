using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using Livet.Commands;
using PortFolion.Core;
using Houzkin.Tree;
using Houzkin.Architecture;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Windows.Input;
using Livet.EventListeners.WeakEvents;

namespace PortFolion.ViewModels {
	public class ListviewModel : ViewModel {
		static WeakReference<ListviewModel> _inst = new WeakReference<ListviewModel>(new ListviewModel());
		public static ListviewModel Instance {
			get {
				ListviewModel vm;
				if(!_inst.TryGetTarget(out vm)) {
					vm = new ListviewModel();
					_inst = new WeakReference<ListviewModel>(vm);
				}
				return vm;
			}
		}
		RootCollection Model;
		private ListviewModel() {
			Model = RootCollection.Instance;
			//Model.CollectionChanged += CollectionChanged;
			this.CompositeDisposable.Add(new CollectionChangedWeakEventListener(Model, CollectionChanged));


			//this.CompositeDisposable.Add(new LivetWeakEventListener<EventHandler<DateTimeSelectedEventArgs>, DateTimeSelectedEventArgs>(
			//	a => a,
			//	h => this.dtr.DateTimeSelected += h,
			//	h => this.dtr.DateTimeSelected -= h,
			//	(o, e) => SetCurrentDate(e.SelectedDateTime)));

			totalRiskFund = RootCollection.Instance.LastOrDefault();
			if (totalRiskFund != null) {
				CurrentDate = totalRiskFund.CurrentDate;
				Path = totalRiskFund.Path;
			}else {
				CurrentDate = null;//DateTime.Today;
				Path = Enumerable.Empty<string>();
			}
			this.RefreshHistory();
		}

		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			dtr.Refresh();
			TotalRiskFundNode rt;
			switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				rt = e.NewItems[0] as TotalRiskFundNode;
				if (rt == null) goto default;
				break;
			default:
				rt = Model.LastOrDefault(a => a.CurrentDate <= (this.CurrentDate ?? DateTime.Today)) ?? Model.LastOrDefault();
				break;
			}
			if (totalRiskFund == rt) return;
			if(rt == null) {
				totalRiskFund = null;
				CurrentDate = null;// DateTime.Today;
				Path = Enumerable.Empty<string>();
				Refresh();
			}else {
				totalRiskFund = rt;
				SetCurrentDate(totalRiskFund.CurrentDate);
			}
		}

		public DateTime? CurrentDate { get; private set; }
		TotalRiskFundNode _trfn;
		TotalRiskFundNode totalRiskFund {
			get { return _trfn; }
			set {
				if (value == _trfn) return;
				_trfn = value;
				root = CommonNodeVM.Create(_trfn);
			}
		}
		CommonNodeVM root;
		public CommonNodeVM Root => root;

		public IEnumerable<string> Path { get; private set; }

		public void RefreshHistory() {
			_history = RootCollection.GetNodeLine(new NodePath<string>(Path)).Select(a => CommonNodeVM.Create(a.Value));
			this.RaisePropertyChanged(nameof(History));
		}
		IEnumerable<CommonNodeVM> _history = null;
		public IEnumerable<CommonNodeVM> History
			=> _history;
		
		public void SetCurrentDate(DateTime date) {
			date = date.Date;
			if (CurrentDate == date) return;
			var c = RootCollection.Instance.LastOrDefault(a => date <= a.CurrentDate) ?? RootCollection.Instance.LastOrDefault();
			if (c == null) {
				if (totalRiskFund == null) {
					CurrentDate = date;//notify
					RaisePropertyChanged(nameof(CurrentDate));
				}
				return;
			}
			totalRiskFund = c;//notify
			CurrentDate = totalRiskFund.CurrentDate;//notify
			dtr.SelectAt(totalRiskFund.CurrentDate);
			
			if (Path.Any() && totalRiskFund.Levelorder().Any(a => a.Path.SequenceEqual(Path))) {
				RaisePropertyChanged(nameof(Root));
				RaisePropertyChanged(nameof(CurrentDate));
				ExpandCurrentNode();
				return;
			}
			Path = totalRiskFund.SearchNodeOf(this.Path)?.Path ?? Enumerable.Empty<string>();
			Refresh();
		}
		public void SetPath(IEnumerable<string> path) {
			if (path.SequenceEqual(Path)) return;
			Path = path;

			RaisePropertyChanged(nameof(this.Path));
			RefreshHistory();
			RaisePropertyChanged(nameof(this.CurrentDate));
			ExpandCurrentNode();
		}
		public void Refresh() {
			RaisePropertyChanged(nameof(this.Root));
			RaisePropertyChanged(nameof(this.Path));
			RefreshHistory();
			RaisePropertyChanged(nameof(this.CurrentDate));
			ExpandCurrentNode();
		}
		#region date
		ListenerCommand<DateTime> addNewRootCommand;
		public ICommand AddNewRootCommand => addNewRootCommand = new ListenerCommand<DateTime>(d => {
			var r = RootCollection.GetOrCreate(d);
			if (string.IsNullOrEmpty(r.Name)) r.Name = "総リスク資産";
			//dtr.SelectAt(d);
			this.SetCurrentDate(d);
			
		});

		DateTreeRoot dtr = new DateTreeRoot();
		public IEnumerable<DateTree> DateList => dtr.Children;
		//void selectedDateList(DateTime date) {
		//	this.SetCurrentDate(date);
		//}
		//void selectDateListItem(DateTime date) {
		//	dtr.SelectAt(date);
		//}
		#endregion

		#region tree
		public void ExpandCurrentNode() {
			if (!this.Path.Any()) return;
			var c = Root.Levelorder().FirstOrDefault(a => a.Path.SequenceEqual(this.Path));
			foreach (var n in c.Upstream()) n.IsExpand = true;
		}
		public void ExpandAllNode() {
			if (Root != null)
				foreach (var n in Root.Levelorder())
					n.IsExpand = true;
		}
		public void CloseAllNode() {
			if (Root != null)
				foreach (var n in Root.Levelorder())
					n.IsExpand = false;
		}
		#endregion

	}
}
