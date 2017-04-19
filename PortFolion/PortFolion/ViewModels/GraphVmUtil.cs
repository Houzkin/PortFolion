using Houzkin;
using Houzkin.Architecture;
using Houzkin.Tree;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Livet;
using Livet.Commands;
using Livet.EventListeners.WeakEvents;
using Livet.Messaging;
using PortFolion.Core;
using PortFolion.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PortFolion.ViewModels {
	public enum Period {
		Weekly,
		Monthly,
		Quarterly,
		Yearly,
		//EachWrittenDate,
	}
	public enum DividePattern {
		Location,
		Tag,
	}
	public enum TransitionStatus {
		/// <summary>推移</summary>
		BalanceOnly,
		/// <summary>推移のラインと外部キャッシュフローをカラムにして表す</summary>
		SingleCashFlow,
		/// <summary>推移のラインと外部キャッシュフローの累積を表す</summary>
		StackCashFlow,
		/// <summary>損益のみ</summary>
		ProfitLossOnly,
	}

	public class TempValue : SeriesViewModel {
		//public string Title { get; set; }
		public double Amount { get; set; }
		public double Rate { get; set; }
		//public double Invest { get; set; }
		//public NodeType Type { get; set; }
	}
	public class GraphValue {
		/// <summary>残高</summary>
		public double Amount { get; set; }
		/// <summary>外部キャッシュフロー</summary>
		public double Flow { get; set; }
		/// <summary>修正ディーツ法による変動比率</summary>
		public double Dietz { get; set; } = 1;
		public DateTime Date { get; set; }
	}
	public class DateSpan {
		public DateSpan(DateTime date1, DateTime date2) {
			if (date1 <= date2) {
				Start = date1;
				End = date2;
			} else {
				Start = date2;
				End = date1;
			}
		}
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}
	public static class Ext {
		public static IEnumerable<T> Dequeue<T>(this Queue<T> self, Func<T, bool> pred) {
			if (!self.Any()) return Enumerable.Empty<T>();
			var lst = new List<T>();
			while (self.Any() && pred(self.Peek())) {
				lst.Add(self.Dequeue());
			}
			return lst;
		}

		public static IEnumerable<GraphValue> ToGraphValues(this Dictionary<DateTime, CommonNode> src, Period period) {
			if (src == null || !src.Any()) return Enumerable.Empty<GraphValue>();
			var ax = Ext.GetTimeAxis(period, src.Keys);
			var srcs = new Queue<KeyValuePair<DateTime, CommonNode>>(src);
			//var lst = new List<GraphValue>();

			var rslt = ax.Scan(new GraphValue(), (prev, ds) => {
				var gv = new GraphValue() { Date = ds.End };
				var tmp = srcs.Dequeue(a => ds.Start <= a.Key && a.Key <= ds.End);
				if (tmp.IsEmpty()) {
					gv.Amount = prev.Amount;
				} else {
					//期初時価総額
					var st = prev.Amount != 0 ? prev.Amount : tmp.First().Value.Amount;
					//期末時価総額
					gv.Amount = tmp.Last().Value.Amount;
					//キャッシュフローを持つ要素
					var fl = tmp.Where(a => a.Value.InvestmentValue != 0).ToArray();
					//キャッシュフロー合計
					gv.Flow = fl.Sum(a => a.Value.InvestmentValue);

					var vc = gv.Amount - st - gv.Flow;

					var cf = fl.Aggregate(0D,
						(pr, cu) => pr + cu.Value.InvestmentValue * (ds.End - cu.Key).Days / (ds.End - ds.Start).Days);

					var rst = st + cf;
					if (rst != 0)
						gv.Dietz = vc / rst;
				}
				return gv;
			});
			return rslt.ToArray();
		}
		static IEnumerable<DateSpan> GetTimeAxis(Period period, IEnumerable<DateTime> dates) {
			DateTime start = dates.Min();
			DateTime end = dates.Max();
			switch (period) {
			case Period.Weekly:
				var wkax = Ext.weeklyAxis(start, end).ToArray();
				return wkax.Zip(wkax.Select(b => b.AddDays(-7)), (a, b) => new DateSpan(a, b));
			case Period.Monthly:
				var mtax = Ext.monthlyAxis(start, end).ToArray();
				return mtax.Zip(
					mtax.Select(a => new DateTime(a.Year, a.Month, 1)), (a, b) => new DateSpan(a, b));
			case Period.Quarterly:
				var qtax = Ext.quarterlyAxis(start, end).ToArray();
				return qtax.Zip(
					qtax.Select(a => new DateTime(a.Year, a.Month, 1).AddMonths(-2)), (a, b) => new DateSpan(a, b));
			case Period.Yearly:
				var yrax = Ext.yearlyAxis(start, end).ToArray();
				return yrax.Zip(yrax.Select(a => new DateTime(a.Year, 1, 1)), (a, b) => new DateSpan(a, b));
			default:
				var erd = dates.Take(1).Concat(dates.Select(a => a.AddDays(1)));
				return dates.Zip(erd, (a, b) => new DateSpan(a, b));
				//return Enumerable.Empty<DateSpan>();
			}
		}
		#region date axis static method
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
		/// <summary>現在のノードから指定した階層だけ下位のノードを返す。</summary>
		public static IEnumerable<CommonNode> TargetLevels(this CommonNode cur, int tgtLv) {
			var cd = cur.NodeIndex().CurrentDepth;
			var ch = cur.Height();
			var tg = Math.Min(ch, tgtLv) + cd;
			return cur.Levelorder()
				.SkipWhile(a => a.NodeIndex().CurrentDepth < tg)
				.TakeWhile(a => a.NodeIndex().CurrentDepth == tg);
		}
		/// <summary>指定した項目種別で括る。</summary>
		public static IEnumerable<TempValue> MargeNodes(this CommonNode current, int tgtLv, DividePattern div) {
			return current.TargetLevels(tgtLv).MargeNodes(div);
		}
		private class CashNodeCountedTempValue : TempValue {
			public int CashCount { get; set; }
		}
		public static IEnumerable<TempValue> MargeNodes(this IEnumerable<CommonNode> collection, DividePattern div) {
			Func<CommonNode, string> DivFunc;
			switch (div) {
			case DividePattern.Location:
				DivFunc = c => c.Name;
				break;
			default:
				DivFunc = c => c.Tag.TagName;
				break;
			}
			var ttl = (double)(collection.Sum(a => (a.Amount)));
			if (ttl == 0) return Enumerable.Empty<TempValue>();
			return collection
				.ToLookup(DivFunc)
				.Select(a => {
					var tv = new CashNodeCountedTempValue();
					tv.Title = a.Key;
					tv.Amount = a.Sum(b => b.Amount);
					tv.Rate = ((double)tv.Amount / (double)ttl);
					tv.CashCount = a.Count(b => b.GetNodeType() == NodeType.Cash);
					return tv;
				})
				.GroupBy(a => a.CashCount)
				.OrderBy(a => a.Key)
				.SelectMany(a => a.OrderByDescending(b => b.Amount));
			//.OrderBy(a => a.CashCount);

		}
		public static IEnumerable<Color> BrushColors() {
			//object obj = ColorConverter.ConvertFromString("#51000000");
			//SolidColorBrush ret = new SolidColorBrush((System.Windows.Media.Color)obj);
			return new List<Color>() {
				Color.FromRgb(17,140,18),
				Color.FromRgb(214,54,96),
				Color.FromRgb(59,67,255),
				Color.FromRgb(167,68,227),
				Color.FromRgb(237,103,30),

				Color.FromRgb(76,143,44),
				Color.FromRgb(44,50,143),
				Color.FromRgb(248,63,27),
				Color.FromRgb(130,16,142),
				Color.FromRgb(21,157,184),

				Color.FromRgb(201,15,41),
				Color.FromRgb(216,105,26),
				Color.FromRgb(7,109,48),
				Color.FromRgb(73,13,147),
				Color.FromRgb(221,39,124),

				Color.FromRgb(225,116,223),
				Color.FromRgb(98,183,189),
				Color.FromRgb(132,79,175),
				Color.FromRgb(255,239,0),
				Color.FromRgb(175,176,0),

				Color.FromRgb(199,27,76),
				Color.FromRgb(170,119,10),
				Color.FromRgb(49,226,239),
				Color.FromRgb(33,145,242),
				Color.FromRgb(21,234,152),
			};
		}
	}
}
