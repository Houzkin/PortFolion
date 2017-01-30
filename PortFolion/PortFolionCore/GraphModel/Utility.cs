using Houzkin.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.Core {
	public class TransitionValue<T> {
		public T Value { get; set; }
		public DateTime Date { get; set; }
	}
	internal static class Utility {

		internal static Dictionary<string, CommonNode> mapping(CommonNode node, int targetLevel) {
			targetLevel = node.NodeIndex().CurrentDepth + targetLevel;
			return node.Levelorder()
				.Where(a => a.NodeIndex().CurrentDepth == targetLevel)
				.ToDictionary(k => k.Name);
		}
		internal static Tuple<IEnumerable<IGrouping<DateTime, NodeMap>>, IEnumerable<IGrouping<DateTime, NodeMap>>> split(IEnumerable<IGrouping<DateTime, NodeMap>> self, DateTime split) {
			return Tuple.Create(
				self.TakeWhile(a => a.Key <= split),
				self.SkipWhile(a => a.Key <= split));
		}
		internal static IEnumerable<IEnumerable<T>> Separate<T>(this IEnumerable<T> src, Func<T, bool> predicate) {
			List<T> list = new List<T>();
			foreach (var e in src) {
				list.Add(e);
				if (predicate(e)) {
					yield return list;
					list.Clear();
				}
			}
			if (list.Any()) yield return list;
		}
		#region static method
		/// <summary>週末日</summary>
		internal static IEnumerable<DateTime> weeklyAxis(DateTime start, DateTime end) {
			DateTime cur = start.DayOfWeek == DayOfWeek.Sunday ? start : start.AddDays(7 - (int)start.DayOfWeek);
			yield return cur;
			while (cur <= end) {
				cur = cur.AddDays(7);
				yield return cur;
			}
		}
		/// <summary>月末日</summary>
		internal static IEnumerable<DateTime> monthlyAxis(DateTime start, DateTime end) {
			var c = EndOfMonth(start);
			yield return c;
			while (c <= end) {
				c = NextEndOfMonth(c, 1);
				yield return c;
			}
		}
		/// <summary>四半期末日</summary>
		internal static IEnumerable<DateTime> quarterlyAxis(DateTime start, DateTime end) {
			int q = start.Month / 3;
			var c = EndOfMonth(new DateTime(start.Year, (q + 1) * 3, 1));
			yield return c;
			while (c <= end) {
				c = NextEndOfMonth(c, 3);
				yield return c;
			}
		}
		/// <summary>年末日</summary>
		internal static IEnumerable<DateTime> yearlyAxis(DateTime start, DateTime end) {
			var c = new DateTime(start.Year, 12, 31);
			yield return c;
			while (c <= end) {
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
		static DateTime NextEndOfMonth(DateTime dt, int month) {
			var d = new DateTime(dt.Year, dt.Month, 1).AddMonths(month);
			return EndOfMonth(d);
		}
		#endregion
	}
}
