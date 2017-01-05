using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;

namespace PortFolion.Core {
	/// <summary>表示用に補正された時系列を管理する</summary>
	public class CompensatedTimeLine : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public DividePattern Divide { get; set; }
		public Period TimePeriod { get; set; }
		public int TargetLevel { get; set; }
		public int MaxElement { get; set; }

		public CommonNode CurrentNode { get; set; }
		public IEnumerable<DateTime> TimeAxis { get; private set; }
		public IEnumerable<KeyValuePair<DateTime,IEnumerable<long>>> ElementsValue { get; private set; }
		/// <summary>一次元にセグメント、二次元に時系列</summary>
		public Dictionary<string,Dictionary<DateTime,CommonNode>> SegmElm {
			get {
				return _nodes
					.ToLookup(a => a.Name)
					.ToDictionary(
						a => a.Key,
						b => b.ToDictionary(c => c.Time, d => d.Node));
			}
		}
		public Dictionary<DateTime,Dictionary<string,CommonNode>> TimeElm {
			get {
				return _nodes
					.ToLookup(a => a.Time)
					.ToDictionary(
						a => a.Key,
						b => b.ToDictionary(c => c.Name, d => d.Node));
			}
		}
		private IEnumerable<tempSection> _nodes;
		class tempSection {
			public DateTime Time { get; set; }
			public TagInfo Tag { get; set; }
			public CommonNode Node { get; set; }
			public string Name { get { return Node.Name; } }
		}
		void setNodes() { 
			int curLv = CurrentNode.NodeIndex().CurrentDepth + TargetLevel;
			Func<CommonNode, int, Dictionary<string, CommonNode>> tbl =
				(cn, lv) => cn.Levelorder()
					.Where(a => a.NodeIndex().CurrentDepth == lv)
					.ToDictionary(k => k.Name);
			//TimeElm
			var d = RootCollection.GetNodeLine(CurrentNode.Path)
				.ToDictionary(
					k => (k.Root() as TotalRiskFundNode).CurrentDate,
					v => tbl(v,curLv));
			var nodes = from tx in d
						from sx in tx.Value
						select new tempSection() {
							Time = tx.Key,
							Tag = sx.Value.Tag,
							Node = sx.Value
						};
			_nodes = nodes.ToArray();

		}

		public void Refresh() {

		}
		private IEnumerable<DateTime> GetTimeAxis() {
			var keys = RootCollection.Instance.Keys;
			if (!keys.Any()) return keys;
			DateTime ls = keys.Last();
			DateTime fs = keys.First();
			switch (TimePeriod) {
			case Period.Weekly:
				return weeklyAxis(fs, ls);
			case Period.Monthly:
				return monthlyAxis(fs, ls);
			case Period.Quarterly:
				return quarterlyAxis(fs, ls);
			case Period.Yearly:
				return yearlyAxis(fs, ls);
			default:
				return Enumerable.Empty<DateTime>();
			}
		}
		IEnumerable<DateTime> weeklyAxis(DateTime start, DateTime end) {
			DateTime cur = start.DayOfWeek == DayOfWeek.Sunday ? start : start.AddDays(7 - (int)start.DayOfWeek);
			yield return cur;
			while(cur<= end) {
				cur = cur.AddDays(7);
				yield return cur;
			}
		}
		/// <summary>月終わり</summary>
		IEnumerable<DateTime> monthlyAxis(DateTime start, DateTime end) {
			var c = EndOfMonth(start);
			yield return c;
			while(c<=end) {
				c = NextEndOfMonth(c, 1);
				yield return c;
			} 
		}
		/// <summary>四半期終わり</summary>
		IEnumerable<DateTime> quarterlyAxis(DateTime start, DateTime end) {
			int q = start.Month / 3;
			var c = EndOfMonth(new DateTime(start.Year, (q + 1) * 3, 1));
			yield return c;
			while(c<=end){
				c = NextEndOfMonth(c, 3);
				yield return c;
			} 
		}
		IEnumerable<DateTime> yearlyAxis(DateTime start, DateTime end) {
			var c = new DateTime(start.Year, 12, 31);
			yield return c;
			while(c<=end) {
				c = c.AddYears(1);
				yield return c;
			} 
		}
		static int DaysInMonth(DateTime dt) {
			return DateTime.DaysInMonth(dt.Year, dt.Month);
		}
		static DateTime EndOfMonth(DateTime dt) {
			return new DateTime(dt.Year, dt.Month, DaysInMonth(dt));
		}
		static DateTime NextEndOfMonth(DateTime dt,int month) {
			var d = new DateTime(dt.Year, dt.Month, 1).AddMonths(month);
			return EndOfMonth(d);
		}
	}
}
