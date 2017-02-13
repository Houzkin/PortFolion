using Houzkin.Tree;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.ViewModels {
	
	
	public class DateTree : ObservableTreeNode<DateTree> ,INotifyPropertyChanged{

		public virtual DateTime? Date {
			get {
				var dd = this.Levelorder().Where(a => !a.Children.Any()).Select(a => a.Date).OrderBy(a => a);
				return dd.Any() ? dd.Last() : null;
			}
		}
		public virtual string Display { get; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
		bool _isExpand;
		public bool IsExpand {
			get { return _isExpand; }
			set {
				if (_isExpand == value) return;
				_isExpand = value;
				RaisePropertyChanged();
			}
		}
		bool _isSelected;
		public bool IsSelected {
			get { return _isSelected; }
			set {
				if (_isSelected == value) return;
				_isSelected = value;
				foreach (var t in this.Upstream()) t.IsExpand = true;
				RaisePropertyChanged();
				var d = this.Date;
				if (d != null) {
					this.Upstream()
						.OfType<DateTreeRoot>()
						.SingleOrDefault()?.DateTimeSelected?.Invoke((DateTime)d);
				}
			}
		}
	}
	public class DateTreeLeaf : DateTree {
		public DateTreeLeaf(DateTime date) { _date = date; }
		DateTime _date;
		public override DateTime? Date => _date;
		public override string Display {
			get {
				return _date.ToString("dd");
			}
		}
	}
	public class DateTreeNode : DateTree {
		string _display;
		public DateTreeNode(string displayName) { _display = displayName; }
		public override string Display => _display;
	}

	public class DateTreeRoot : DateTree {
		readonly INotifyCollectionChanged src;
		readonly IEnumerable<TotalRiskFundNode> items;
		public DateTreeRoot(IEnumerable<TotalRiskFundNode> dateCollection) {
			src = dateCollection as INotifyCollectionChanged;
			items = src as IEnumerable<TotalRiskFundNode>;
			src.CollectionChanged += reAssembleTree;
			foreach(var i in items)
				assembleTree(i.CurrentDate);
		}
		void assembleTree(DateTime date) {
			var y = Children.FirstOrDefault(a => a.Display == date.Year.ToString());
			if(y==null) {
				y = new DateTreeNode(date.Year.ToString());
				this.AddChild(y);
			}
			var m = y.Children.FirstOrDefault(a => a.Display == date.Month.ToString());
			if (m == null) {
				m = new DateTreeNode(date.Month.ToString());
				y.AddChild(m);
			}
			if(!m.Children.Any(a=>a.Date == date))
				m.AddChild(new DateTreeLeaf(date));
		}
		void reAssembleTree(object s, NotifyCollectionChangedEventArgs e) {
			this.ClearChildren();
			foreach (var i in items) assembleTree(i.CurrentDate);
		}
		public Action<DateTime> DateTimeSelected;
		public void SelectAt(DateTime date) {
			var n = this.Levelorder().OfType<DateTreeLeaf>().SingleOrDefault(a => a.Date == date);
			if (n != null) {
				foreach (var nd in n.Upstream()) nd.IsExpand = true;
				n.IsSelected = true;
			}
		}
	}
}
