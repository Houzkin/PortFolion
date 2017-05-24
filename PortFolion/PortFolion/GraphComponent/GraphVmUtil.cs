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
	/// <summary>期間</summary>
	public enum Period {
		Weekly,
		Monthly,
		Quarterly,
		Yearly,
	}
	/// <summary>識別方法</summary>
	public enum DividePattern {
		Location,
		Tag,
	}
	/// <summary>残高に対するキャッシュフローの扱い方</summary>
	public enum BalanceCashFlow {
		Ignore,
		Flow,
		Stack,
	}

	public class SeriesValue : SeriesViewModel {
		public double Amount { get; set; }
		public double Rate { get; set; }
	}
	public class PlotValue {
		/// <summary>残高</summary>
		public double Amount { get; set; }
		/// <summary>外部キャッシュフロー</summary>
		public double Flow { get; set; }
		/// <summary>修正ディーツ法による変動比率(利回り)</summary>
		public double Dietz { get; set; } = 0;
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
		public static IEnumerable<IEnumerable<PlotValue>> ToStackGraphValues(this Dictionary<DateTime, CommonNode> src,
			CommonNode cur, Period period, int level, DividePattern divide) {
			var curR = new SeriesValue[] { new SeriesValue() { Title = "" } }.Concat(cur.MargeNodes(level, divide)).ToArray();


			throw new NotImplementedException();
		}
		static IEnumerable<PlotValue> assign(IEnumerable<PlotValue> model,IEnumerable<PlotValue> data) {
			throw new NotImplementedException();
		}
		public static IEnumerable<PlotValue> ToGraphValues(this Dictionary<DateTime, CommonNode> src, Period period) {
			if (src == null || !src.Any()) return Enumerable.Empty<PlotValue>();
			var ax = Ext.GetTimeAxis(period, src.Keys);
			var srcs = new Queue<KeyValuePair<DateTime, CommonNode>>(src);
			//var lst = new List<GraphValue>();

			var rslt = ax.Scan(new PlotValue(), (prev, ds) => {
				var gv = new PlotValue() { Date = ds.End };
				var tmp = srcs.Dequeue(a => ds.Start <= a.Key && a.Key <= ds.End);
				if (tmp.IsEmpty()) {
					gv.Amount = prev.Amount;
				} else {
					//期初時価総額
					var st = prev.Amount != 0 ? prev.Amount : tmp.First().Value.Amount - tmp.First().Value.InvestmentValue;
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
				return wkax.Zip(wkax.Select(b => b.AddDays(-6)), (a, b) => new DateSpan(a, b));
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
		static IEnumerable<DateTime> weeklyAxis(DateTime start, DateTime end) {
			DateTime cur = start.DayOfWeek == DayOfWeek.Sunday ? start : start.AddDays(7 - (int)start.DayOfWeek);
			yield return cur;
			while(cur < end) {
				cur = cur.AddDays(7);
				yield return cur;
			}
		}
		/// <summary>月末日</summary>
		static IEnumerable<DateTime> monthlyAxis(DateTime start, DateTime end) {
			var c = EndOfMonth(start);
			yield return c;
			while (c < end) {
				c = NextEndOfMonth(c, 1);
				yield return c;
			}
		}
		/// <summary>四半期末日</summary>
		static IEnumerable<DateTime> quarterlyAxis(DateTime start, DateTime end) {
			int q = start.Month / 3;
			var c = EndOfMonth(new DateTime(start.Year, (q + 1) * 3, 1));
			yield return c;
			while (c < end) {
				c = NextEndOfMonth(c, 3);
				yield return c;
			}
		}
		/// <summary>年末日</summary>
		static IEnumerable<DateTime> yearlyAxis(DateTime start, DateTime end) {
			var c = new DateTime(start.Year, 12, 31);
			yield return c;
			while (c < end) {
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
		public static IEnumerable<SeriesValue> MargeNodes(this CommonNode current, int tgtLv, DividePattern div) {
			return current.TargetLevels(tgtLv).MargeNodes(div);
		}
		private class CashNodeCountedSeriesValue : SeriesValue {
			public int CashCount { get; set; }
		}
		public static IEnumerable<SeriesValue> MargeNodes(this IEnumerable<CommonNode> collection, DividePattern div) {
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
			if (ttl == 0) return Enumerable.Empty<SeriesValue>();
			return collection
				.ToLookup(DivFunc)
				.Select(a => {
					var tv = new CashNodeCountedSeriesValue();
					tv.Title = a.Key;
					tv.Amount = a.Sum(b => b.Amount);
					tv.Rate = ((double)tv.Amount / (double)ttl);
					tv.CashCount = a.Count(b => b.GetNodeType() == NodeType.Cash);
					return tv;
				})
				.GroupBy(a => a.CashCount)
				.OrderBy(a => a.Key)
				.SelectMany(a => a.OrderByDescending(b => b.Amount));

		}
		static Random rdm = new Random();
		static int rdmIdx = -1;
		public static void ResetColorIndex() {
			rdmIdx = rdm.Next(BrushColors().Count);
		}
		public static List<Color> BrushOrder() {
			if (rdmIdx < 0) ResetColorIndex();
			return BrushColors().Repeat().Skip(rdmIdx).Take(BrushColors().Count).ToList();
		}
		static List<Color> colors;
		public static List<Color> BrushColors() {
			//object obj = ColorConverter.ConvertFromString("#51000000");
			//SolidColorBrush ret = new SolidColorBrush((System.Windows.Media.Color)obj);
			return colors = colors ?? new List<Color>() {
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
