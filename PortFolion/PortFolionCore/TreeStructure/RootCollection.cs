using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Houzkin.Tree;
using PortFolion.IO;

namespace PortFolion.Core {
	public class RootCollection : ObservableCollection<TotalRiskFundNode> ,IReadOnlyDictionary<DateTime,TotalRiskFundNode>{

		private RootCollection() : base(HistoryIO.ReadRoots().OrderBy(a=>a.CurrentDate)) { }

		public static RootCollection Instance { get; } = new RootCollection();

		public static TotalRiskFundNode GetOrCreate(DateTime date) {
			date = new DateTime(date.Year, date.Month, date.Day);
			if (!Instance.Keys.Contains(date)) 
				Instance.Add(new TotalRiskFundNode() { CurrentDate = date });
			return Instance[date];
		}
		/// <summary>指定した時間において、指定した位置に存在するノードを取得する</summary>
		public static CommonNode GetNode(NodePath<string> path, DateTime date) {
			var nn = GetNodeLine(path).ToDictionary(a => (a.Root() as TotalRiskFundNode).CurrentDate);
			return nn.LastOrDefault(a => a.Key <= date).Value;
		}
		public static IEnumerable<CommonNode> GetNodeLine(NodePath<string> path) {
			return Instance.Values
				.SelectMany(
					a => a.Evolve(
						b => b.Path.Except(path).Any() ? null : b.Children,
						(c, d) => c.Concat(d)))
				.Where(e => e.Path.SequenceEqual(path));
		}
		/// <summary>指定した時間を含む指定位置の一連のノードを取得する</summary>
		public static IEnumerable<CommonNode> GetNodeLine(NodePath<string> path,DateTime currentTenure) {
			
			var lne = GetNodeLine(path)
				.ToDictionary(a => ((TotalRiskFundNode)a.Root()).CurrentDate);
			var ttl = Instance
				.Keys.ToArray();

			var aft = lne.Keys.Where(a => currentTenure <= a);
			var aftSel = ttl.Where(a => currentTenure <= a);

			var bef = lne.Keys.Where(a => a < currentTenure);
			var befSel = ttl.Where(a => a < currentTenure);

			var lst = aft.Zip(aftSel, (a, b) => new { Aft = a, AftSel = b })
				.LastOrDefault(a => a.Aft == a.AftSel);

			var fst = bef.Reverse()
				.Zip(befSel.Reverse(), (a, b) => new { Bef = a, BefSel = b })
				.LastOrDefault(a => a.Bef == a.BefSel);

			var result = lne.AsEnumerable();
			if (fst != null) result = result.SkipWhile(a => a.Key < fst.Bef);
			if (lst != null) result = result.TakeWhile(a => lst.Aft <= a.Key);

			return result.Select(a => a.Value);
		}
		internal static bool CanChangeNodeName(NodePath<string> path,string name) {
			return GetNodeLine(path)
				.Select(a => a.Siblings())
				.All(a => a.All(b => b.Name != name));
		}
		internal static void ChangeNodeName(NodePath<string> path,string newName) {
			foreach (var t in GetNodeLine(path)) {
				if (t.Siblings().All(a => a.Name != newName))
					t.Name = newName;
			}
		}
		internal static void ChangeNodeTag(NodePath<string> path,string newTag) {
			var tg = TagInfo.GetWithAdd(newTag);
			foreach (var t in GetNodeLine(path)) t.Tag = tg;
		}
		internal static void ChangeNodeTag(NodePath<string> path,string newTag,DateTime current) {
			var tg = TagInfo.GetWithAdd(newTag);
			foreach (var t in GetNodeLine(path, current)) t.Tag = tg;
		}
		internal static void RemoveNodeTag(NodePath<string> path) {
			foreach (var t in GetNodeLine(path)) t.Tag = null;
		}
		internal static void RemoveNodeTag(NodePath<string> path,DateTime current) {
			foreach (var t in GetNodeLine(path, current)) t.Tag = null;
		}

		#region インスタンス
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
			base.InsertItem(index, item);
			if (!checkSeq(index)) DateTimeChange(item.CurrentDate);
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
				key = new DateTime(key.Year, key.Month, key.Day);
				if (!Keys.Contains(key)) throw new KeyNotFoundException();
				return Items.First(a => a.CurrentDate == key);
			}
		}

		public IEnumerable<DateTime> Keys {
			get { return Items.Select(a => a.CurrentDate); }
		}

		public IEnumerable<TotalRiskFundNode> Values {
			get { return Items; }
		}

		public bool ContainsKey(DateTime key) {
			key = new DateTime(key.Year, key.Month, key.Day);
			return Keys.Contains(key);
		}

		public bool TryGetValue(DateTime key, out TotalRiskFundNode value) {
			key = new DateTime(key.Year, key.Month, key.Day);
			if (ContainsKey(key)) {
				value = Items.First(a => a.CurrentDate == key);
				return true;
			} else {
				value = null;
				return false;
			}
		}
		IEnumerator<KeyValuePair<DateTime, TotalRiskFundNode>> IEnumerable<KeyValuePair<DateTime, TotalRiskFundNode>>.GetEnumerator() {
			return Items.Select(a => new KeyValuePair<DateTime, TotalRiskFundNode>(a.CurrentDate, a)).GetEnumerator();
		}
		#endregion
	}
}
