using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;

namespace PortFolion.Core {
	public class NodeMap {
		public DateTime Time { get; set; }
		public TagInfo Tag { get { return Node.Tag; } }
		public CommonNode Node { get; set; }
		public string Name { get { return Node.Name; } }
	}
	/// <summary>表示用に補正された時系列を管理する</summary>
	public class CompensatedTimeLine : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public DividePattern Divide { get; set; }
		public Period TimePeriod { get; set; }
		public int TargetLevel { get; set; }
		public int MaxElement { get; set; }

		public CommonNode CurrentNode { get; set; }
		public IEnumerable<DateTime> TimeAxis { get; private set; }

		

		/// <summary>一次元にセグメント、二次元に時系列</summary>
		public Dictionary<string,Dictionary<DateTime,CommonNode>> RowSegmentElement {
			get {
				return CurrentMap
					.ToLookup(a => a.Name)
					.ToDictionary(
						a => a.Key,
						b => b.ToDictionary(c => c.Time, d => d.Node));
			}
		}
		/// <summary>一次元に時系列、二次元にセグメント</summary>
		public Dictionary<DateTime,Dictionary<string,CommonNode>> RowTimeElement {
			get {
				return CurrentMap
					.ToLookup(a => a.Time)
					.ToDictionary(
						a => a.Key,
						b => b.ToDictionary(c => c.Name, d => d.Node));
			}
		}
		public IEnumerable<NodeMap> CurrentMap { get; private set; }
		
		void refresh() {
			//TimeElm
			var d = RootCollection.GetNodeLine(CurrentNode.Path)
				.ToDictionary(
					k => (k.Root() as TotalRiskFundNode).CurrentDate,
					v => mapping(v, TargetLevel));//tbl(v,curLv));
			var nodes = from tx in d
						from sx in tx.Value
						select new NodeMap() {
							Time = tx.Key,
							Node = sx.Value,
						};
			CurrentMap = nodes.ToArray();
			TimeAxis = GetTimeAxis().ToArray();
		}

		public Dictionary<string,IEnumerable<long>> SegmentElement {
			get {
				var t = CurrentMap
					.ToLookup(a => a.Name)
					.ToDictionary(a => a.Key, b => marge(b.GroupBy(c => c.Time)));
				return t;
			}
		}

		private IEnumerable<DateTime> GetTimeAxis() {
			var nds = RootCollection
				.GetNodeLine(CurrentNode.Path)
				.Select(a => (a as TotalRiskFundNode).CurrentDate);
			if (!nds.Any()) return nds;
			DateTime ls = nds.Last();
			DateTime fs = nds.First();
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
		//private IEnumerable<Dictionary<DateTime,long>> timeMarge(IEnumerable<IGrouping<DateTime,NodeMap>> src) {
			
		//	foreach(var g in src) {

		//	}
		//}
		private IEnumerable<long> marge(IEnumerable<IGrouping<DateTime,NodeMap>> src) {
			IEnumerable<long> tmp = Enumerable.Empty<long>();
			var s = src;
			foreach(var ax in TimeAxis) {
				var tpl = Split(s, ax);
				var r = tpl.Item1.LastOrDefault()?.LastOrDefault()?.Node.Amount;
				yield return r ?? 0;
				s = tpl.Item2;
			}
		}
		
		private static Dictionary<string,CommonNode> mapping(CommonNode node, int targetLevel) {
			targetLevel = node.NodeIndex().CurrentDepth + targetLevel;
			return node.Levelorder()
				.Where(a => a.NodeIndex().CurrentDepth == targetLevel)
				.ToDictionary(k => k.Name);
		}
		private static Tuple<IEnumerable<IGrouping<DateTime,NodeMap>>,IEnumerable<IGrouping<DateTime,NodeMap>>> Split(IEnumerable<IGrouping<DateTime,NodeMap>> self, DateTime split) {
			return Tuple.Create(
				self.TakeWhile(a => a.Key <= split), 
				self.SkipWhile(a => a.Key <= split));
		}
		#region static method
		/// <summary>週末日</summary>
		static IEnumerable<DateTime> weeklyAxis(DateTime start, DateTime end) {
			DateTime cur = start.DayOfWeek == DayOfWeek.Sunday ? start : start.AddDays(7 - (int)start.DayOfWeek);
			yield return cur;
			while(cur<= end) {
				cur = cur.AddDays(7);
				yield return cur;
			}
		}
		/// <summary>月末日</summary>
		static IEnumerable<DateTime> monthlyAxis(DateTime start, DateTime end) {
			var c = EndOfMonth(start);
			yield return c;
			while(c<=end) {
				c = NextEndOfMonth(c, 1);
				yield return c;
			} 
		}
		/// <summary>四半期末日</summary>
		static IEnumerable<DateTime> quarterlyAxis(DateTime start, DateTime end) {
			int q = start.Month / 3;
			var c = EndOfMonth(new DateTime(start.Year, (q + 1) * 3, 1));
			yield return c;
			while(c<=end){
				c = NextEndOfMonth(c, 3);
				yield return c;
			} 
		}
		/// <summary>年末日</summary>
		static IEnumerable<DateTime> yearlyAxis(DateTime start, DateTime end) {
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
		#endregion
	}
}
