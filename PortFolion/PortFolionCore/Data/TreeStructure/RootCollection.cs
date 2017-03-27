﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Houzkin.Tree;
using PortFolion.IO;
using Houzkin.Collections;
using System.Collections.Specialized;
using Houzkin;

namespace PortFolion.Core {
	public static class ExtBugs {
		public static U convert<T, U>(T self, Func<T, U> generator, Action<U, U> addAction)
		where T : IReadOnlyTreeNode<T> {
			if (generator == null) throw new ArgumentNullException("generator");
			if (addAction == null) throw new ArgumentNullException("addAction");
			var t = self.Postorder().Select(x => Tuple.Create(x, generator(x)));
			var vst = new ElementScroller<Tuple<T, U>>(t);
			foreach (var tr in vst.GetSequence()) {
				vst.MoveTo(tr)
					.MaybeNext(x => x.Item1.Children.Contains(tr.Item1))
					.TrueOrNot(r => addAction(r.Current.Item2, tr.Item2));
			}
			return vst.Current.Item2;
		}
		public static U ConvertTest<T, U>(this T self, Func<T, U> generator, Action<U, U> addAction)
		where T : IReadOnlyTreeNode<T> {
			return convert(self, generator, addAction);
		}
	}
	public class RootCollection : ObservableCollection<TotalRiskFundNode> {//,IReadOnlyDictionary<DateTime,TotalRiskFundNode>{


		public static RootCollection Instance { get; } = new RootCollection();

		public static TotalRiskFundNode GetOrCreate(DateTime date) {
			date = new DateTime(date.Year, date.Month, date.Day);
			if (!Instance.Keys.Contains(date)) {
				var ins = Instance.Keys.Where(a => a < date);
				var pns = Instance.Keys.Where(a => a > date);
				if (!ins.Any() && !pns.Any()) {
					var r = new TotalRiskFundNode();
					r.CurrentDate = date;
					Instance.Add(r);
				}else if(ins.Any()) {
					var d = ins.Max();
					var a1 = Instance[d] as CommonNode;
					var a2 = a1.ConvertTest(a => a.Clone(),(a,b)=>a.AddChild(b));

					var trfn = a2 as TotalRiskFundNode;//= ((Instance[d] as CommonNode).Convert(a => a.Clone())) as TotalRiskFundNode;
					trfn.RemoveDescendant(a => !a.HasPosition && a.GetType() != typeof(FinancialValue));
					trfn.CurrentDate = date;
					Instance.Add(trfn);
				}else if (pns.Any()) {
					var d = pns.Min();
					var trfn = ((Instance[d] as CommonNode).ConvertTest(a => a.Clone(),(a,b)=>a.AddChild(b))) as TotalRiskFundNode;
					trfn.CurrentDate = date;
					Instance.Insert(0, trfn);
				}
			}
			return Instance[date];
		}
		public static Dictionary<DateTime,CommonNode> GetNodeLine(IEnumerable<string> path) {
			return Instance//.Values
				.SelectMany(
					a => a.Evolve(
						b => b.Path.Except(path).Any() ? null : b.Children,
						(c, d) => c.Concat(d)))
				.Where(e => e.Path.SequenceEqual(path))
				.ToDictionary(b => (b.Root() as TotalRiskFundNode).CurrentDate);
		}
		/// <summary>指定した時間において、指定した位置に存在するノードを取得する</summary>
		public static CommonNode GetNode(NodePath<string> path, DateTime date) {
			var nn = GetNodeLine(path);//.ToDictionary(a => (a.Root() as TotalRiskFundNode).CurrentDate);
			return nn.LastOrDefault(a => a.Key <= date).Value;
		}
		
		/// <summary>指定した時間を含む指定位置のポジション単位のノードを取得する</summary>
		public static Dictionary<DateTime,CommonNode> GetNodeLine(NodePath<string> path,DateTime currentTenure) {
			var lne = GetNodeLine(path);
			//指定した日付を含んだノードを取得
			var curNd = lne.LastOrDefault(a => currentTenure <= a.Key);
			if (curNd.Value == null) return new Dictionary<DateTime, CommonNode>();
			
			var bef = lne.TakeWhile(a => a.Value != curNd.Value).Reverse().TakeWhile(a => a.Value.HasPosition);//a => a.Value.Amount != 0);
			var aft = lne.SkipWhile(a => a.Value != curNd.Value).Separate(a => !a.Value.HasPosition).First();//a => a.Value.Amount == 0).First();

			return bef.Concat(aft).OrderBy(a => a.Key).ToDictionary(a => a.Key, b => b.Value);
		}
		/// <summary>指定したパスに該当する全てのノードの名前を変更可能かどうか示す値を取得する。</summary>
		/// <param name="path">変更対象を示すパス</param>
		/// <param name="name">新しい名前</param>
		/// <returns>重複があった日付</returns>
		public static ResultWithValue<IEnumerable<DateTime>> CanChangeNodeName(NodePath<string> path,string name) {
			var src = GetNodeLine(path)
				.Select(a => new { Date = a.Key, Sigls = a.Value.Siblings().Except(new CommonNode[] { a.Value }) })
				.Select(a => new { Date = a.Date, Rst = a.Sigls.All(b => b.Name != name) })
				.ToArray();
			if (src.All(a => a.Rst)) {
				return new ResultWithValue<IEnumerable<DateTime>>(true, Enumerable.Empty<DateTime>());
			}else {
				return new ResultWithValue<IEnumerable<DateTime>>(false, src.Where(a => !a.Rst).Select(a => a.Date));
			}
			//return GetNodeLine(path)
			//	.Values
			//	.Select(a => a.Siblings())
			//	.All(a => a.All(b => b.Name != name));
		}
		/// <summary>指定したパスに該当する全てのノード名を変更する</summary>
		/// <param name="path">変更対象を示すパス</param>
		/// <param name="newName">新しい名前</param>
		/// <returns>変更を行った日付</returns>
		public static IEnumerable<DateTime> ChangeNodeName(NodePath<string> path,string newName) {
			List<DateTime> lst = new List<DateTime>();
			foreach (var t in GetNodeLine(path)) {
				if (t.Value.Siblings().All(a => a.Name != newName)) {
					t.Value.Name = newName;
					lst.Add(t.Key);
				}
			}
			return lst;
		}
		internal static void ChangeNodeTag(NodePath<string> path,string newTag) {
			var tg = TagInfo.GetWithAdd(newTag);
			foreach (var t in GetNodeLine(path).Values) t.Tag = tg;
		}
		internal static void ChangeNodeTag(NodePath<string> path,string newTag,DateTime current) {
			var tg = TagInfo.GetWithAdd(newTag);
			foreach (var t in GetNodeLine(path, current).Values) t.Tag = tg;
		}
		internal static void RemoveNodeTag(NodePath<string> path) {
			foreach (var t in GetNodeLine(path).Values) t.Tag = null;
		}
		internal static void RemoveNodeTag(NodePath<string> path,DateTime current) {
			foreach (var t in GetNodeLine(path, current).Values) t.Tag = null;
		}

		#region インスタンス
		private RootCollection() :base() {
			var itm = HistoryIO.ReadRoots().OrderBy(a => a.CurrentDate);
			foreach (var i in itm) this.Items.Add(i);
		}
		public void Refresh() {
			
			this.Items.Clear();
			
			var itm = HistoryIO.ReadRoots().OrderBy(a => a.CurrentDate);
			foreach (var i in itm) this.Items.Add(i);

			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
		internal void DateTimeChange(DateTime date) {
			var lst = new List<DateTime>(this.Keys);
			var cur = lst.IndexOf(date);
			var idx = lst.FindIndex(a => date < a);
			if (cur == idx) return;
			this.Move(cur, idx);
		}
		bool checkSeq(int index) {
			if (0 < index && this[index - 1].CurrentDate > this[index].CurrentDate) return false;
			if (index < Count-1 && this[index].CurrentDate > this[index + 1].CurrentDate) return false;
			return true;
		}
		protected override void InsertItem(int index, TotalRiskFundNode item) {
			if (ContainsKey(item.CurrentDate)) return;
			var hs = new HashSet<DateTime>(this.Keys);
			hs.Add(item.CurrentDate);
			var idx = new List<DateTime>(hs.OrderBy(a => a)).IndexOf(item.CurrentDate);
			base.InsertItem(idx, item);
			//if (!checkSeq(index)) DateTimeChange(item.CurrentDate);
		}
		protected override void SetItem(int index, TotalRiskFundNode item) {
			if (ContainsKey(item.CurrentDate)) return;
			this[index].MainList = null;
			base.SetItem(index, item);
			if (!checkSeq(index)) DateTimeChange(item.CurrentDate);
		}
		protected override void RemoveItem(int index) {
			this[index].MainList = null;
			base.RemoveItem(index);
		}
		protected override void ClearItems() {
			foreach (var itm in this) itm.MainList = null;
			base.ClearItems();
		}
		public TotalRiskFundNode this[DateTime key] {
			get {
				key = key.Date;// new DateTime(key.Year, key.Month, key.Day);
				if (!this.ContainsKey(key)) throw new KeyNotFoundException();
				return Items.First(a => a.CurrentDate == key);
			}
		}

		public IEnumerable<DateTime> Keys {
			get { return Items.Select(a => a.CurrentDate); }
		}

		//public IEnumerable<TotalRiskFundNode> Values {
		//	get { return Items; }
		//}

		public bool ContainsKey(DateTime key) {
			key = key.Date;// new DateTime(key.Year, key.Month, key.Day);
			return Keys.Contains(key);
		}

		public bool TryGetValue(DateTime key, out TotalRiskFundNode value) {
			key = key.Date; //new DateTime(key.Year, key.Month, key.Day);
			if (ContainsKey(key)) {
				value = Items.First(a => a.CurrentDate == key);
				return true;
			} else {
				value = null;
				return false;
			}
		}
		
		//IEnumerator<KeyValuePair<DateTime, TotalRiskFundNode>> IEnumerable<KeyValuePair<DateTime, TotalRiskFundNode>>.GetEnumerator() {
		//	return Items.Select(a => new KeyValuePair<DateTime, TotalRiskFundNode>(a.CurrentDate, a)).GetEnumerator();
		//}
		#endregion
	}
}
