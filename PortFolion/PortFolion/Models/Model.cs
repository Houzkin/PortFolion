using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Houzkin.Tree;
using PortFolion.Core;
using Livet;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace PortFolion.Models {
	public class Model : NotificationObject {
		/*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */
	}
	public class DateTree : ObservableTreeNode<DateTree> {

		public virtual DateTime? Date {
			get {
				var dd = this.Levelorder().Where(a => !a.Children.Any()).Select(a => a.Date).OrderBy(a => a);
				return dd.Any() ? dd.Last() : null;
			}
		}
			
			
		public virtual string Display { get; }
	}
	public class DateTreeLeaf : DateTree {
		public DateTreeLeaf(DateTime date) { _date = date; }
		DateTime _date;
		public override DateTime? Date => _date;
		public override string Display {
			get {
				return _date.ToString("MM/dd");
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
		public DateTreeRoot(INotifyCollectionChanged dateCollection) {
			src = dateCollection;
			items = src as IEnumerable<TotalRiskFundNode>;
			foreach(var i in items)
				assembleTree(i.CurrentDate);
		}
		void assembleTree(DateTime date) {
			var t = Children.FirstOrDefault(a => a.Display == date.Year.ToString());
			if(t==null) {
				t = new DateTreeNode(date.Year.ToString());
				this.AddChild(t);
			}
			//
		}
	}
}
