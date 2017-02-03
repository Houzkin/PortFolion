using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using Houzkin.Tree.Serialization;
using Houzkin.Xml;
using System.Xml.Serialization;
using System.IO;
using Houzkin;

namespace PortFolion.IO {
	public static class HistoryIO {
		public static void SaveRoots() {
			saveRoots(RootCollection.Instance);
		}
		public static void SaveRoots(DateTime date) {
			SaveRoots(date, date);
		}
		public static void SaveRoots(DateTime since,DateTime until) {
			saveRoots(RootCollection.Instance.Where(a => since <= a.CurrentDate && a.CurrentDate <= until));
		}
		static readonly string _path = AppDomain.CurrentDomain.BaseDirectory + "今まで書き込んだデータ" + Path.PathSeparator;
		static void saveRoots(IEnumerable<TotalRiskFundNode> list) {
			if (!list.Any()) return;
			var serializer = new XmlSerializer(typeof(SerializableNodeMap<CushionNode>));
			foreach (var r in list) {
				var sri = (r as CommonNode).ToSerializableNodeMap(a => a.ToSerialCushion());
				var curPath = _path + r.CurrentDate.Year.ToString() + Path.PathSeparator + r.CurrentDate.ToString("yyyy-MM-dd") + ".xml";
				using (FileStream fs = new FileStream(curPath, FileMode.Create)) {
					serializer.Serialize(fs,sri);
				}
			}
		}
		internal static IEnumerable<TotalRiskFundNode> ReadRoots() {
			return new DirectoryInfo(_path)
				.GetDirectories("*", SearchOption.TopDirectoryOnly)
				.Where(a => ResultWithValue.Of<int>(int.TryParse, a.Name))
				.SelectMany(a => a.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
				.Where(a => ResultWithValue.Of<DateTime>(DateTime.TryParse, a.Name))
				.Select(a => readRoot(a.FullName))
				.Where(a => a != null);
		}
		static TotalRiskFundNode readRoot(string path) {
			SerializableNodeMap<CushionNode> nodes;
			var serializer = new XmlSerializer(typeof(SerializableNodeMap<CushionNode>));
			try {
				using (FileStream fs = new FileStream(path, FileMode.Open)) {
					nodes = serializer.Deserialize(fs) as SerializableNodeMap<CushionNode>;
				}
			} catch {
				nodes = null;
			}
			return nodes?.AssembleTree(a => a.ToInstance()) as TotalRiskFundNode;
		}
	}
}
