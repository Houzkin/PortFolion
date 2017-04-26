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
		protected DateTree() {
			this.Children.CollectionChanged += (o, e) => RaisePropertyChanged(nameof(Children));
		}

		public virtual DateTime? Date {
			get {
				return this.Levelorder().OfType<DateTreeLeaf>().Select(a => a.Date).OrderBy(a => a).LastOrDefault();
				//var dd = this.Levelorder().Where(a => !a.Children.Any()).Select(a => a.Date).OrderBy(a => a);
				//return dd.Any() ? dd.Last() : null;
			}
		}
		public virtual int Number { get; }
		public virtual string Display { get; }

		public event PropertyChangedEventHandler PropertyChanged;

		public void Sort() => this.ChildNodes.Sort(a => a.Number);

		protected void RaisePropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
				RaisePropertyChanged();
			}
		}
		protected virtual void RaiseDateTimeSelected(DateTime date) {
			this.Root().RaiseDateTimeSelected(date);
		}
	}
	public class DateTreeLeaf : DateTree {
		public DateTreeLeaf(DateTime date) {
			_date = date;
			this.PropertyChanged += (o, e) => {
				if(e.PropertyName == nameof(this.IsSelected) && this.IsSelected) {
					foreach (var t in this.Upstream().Skip(1)) t.IsExpand = true;
					//(this.Root() as DateTreeRoot)
					//?.DateTimeSelected
					//?.Invoke(this.Root(), new DateTimeSelectedEventArgs(_date));
					this.RaiseDateTimeSelected(_date);
				}
			};
		}
		DateTime _date;
		public override DateTime? Date => _date;
		public override int Number => _date.Day;
		public override string Display => Number.ToString() + "日";
	}
	public class DateTreeNode : DateTree {
		int _number;
		Func<int, string> _toDisplay;
		public DateTreeNode(int displayName, Func<int,string> toDisplay) {
			_number = displayName;
			_toDisplay = toDisplay;
		}
		public override int Number => _number;
		public override string Display => _toDisplay(Number);
	}

	public class DateTreeRoot : DateTree {
		public DateTreeRoot() {
			Refresh();
		}
		void assembleTree(DateTime date) {
			var y = Children.FirstOrDefault(a => a.Number == date.Year);
			if(y==null) {
				y = new DateTreeNode(date.Year, a => a.ToString() + "年");
				this.AddChild(y);
			}
			var m = y.Children.FirstOrDefault(a => a.Number == date.Month);
			if (m == null) {
				m = new DateTreeNode(date.Month, a => a.ToString() + "月");
				y.AddChild(m);
			}
			if(!m.Children.Any(a=>a.Date == date))
				m.AddChild(new DateTreeLeaf(date));
		}
		
		public void Refresh() {
			var cur = RootCollection.Instance.Select(a => a.CurrentDate);
			var prv = this.Preorder().OfType<DateTreeLeaf>().Select(a => (DateTime)a.Date);
			var ads = cur.Except(prv).ToArray();
			var rmv = prv.Except(cur).ToArray();
			foreach (var d in ads)
				assembleTree(d);
			foreach (var d in rmv)
				foreach (var dd in this.Preorder().OfType<DateTreeLeaf>().Where(a => a.Date == d).ToArray())
					dd.Parent.RemoveChild(dd);
			var rl = this.Levelorder().Skip(1).Where(a => !a.Preorder().OfType<DateTreeLeaf>().Any()).ToArray();
			foreach (var r in rl) r.MaybeRemoveOwn();
			//this.RemoveDescendant(a => !a.Preorder().OfType<DateTreeLeaf>().Any());
			this.Levelorder().ToArray().ForEach(a => a.Sort());
		}
		public event EventHandler<DateTimeSelectedEventArgs> DateTimeSelected;
		public void SelectAt(DateTime date) {
			var n = this.Levelorder().OfType<DateTreeLeaf>().LastOrDefault(a => a.Date == date);
			if (n != null) {
				//foreach (var nd in n.Upstream()) nd.IsExpand = true;
				n.IsSelected = true;
			}
		}
		protected override void RaiseDateTimeSelected(DateTime date) {
			DateTimeSelected?.Invoke(this, new DateTimeSelectedEventArgs(date));
		}
	}
	public class DateTimeSelectedEventArgs : EventArgs {
		public DateTime SelectedDateTime { get; private set; }
		public DateTimeSelectedEventArgs(DateTime dt) { SelectedDateTime = dt; }
	}
}
