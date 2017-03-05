﻿using PortFolion.Core;
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
			SaveRoots(DateTime.MinValue, DateTime.MaxValue);
		}
		public static void SaveRoots(DateTime date) {
			SaveRoots(date, date);
		}
		public static void SaveRoots(DateTime since,DateTime until) {
			saveRoots(RootCollection.Instance.Where(a => since <= a.CurrentDate && a.CurrentDate <= until), since, until);
		}
		static readonly string _path = AppDomain.CurrentDomain.BaseDirectory + "今まで書き込んだデータ" + Path.DirectorySeparatorChar;
		static void saveRoots(IEnumerable<TotalRiskFundNode> list,DateTime since,DateTime until) {
			var pu = pickUp(since, until).Select(a => a.FullName);
			var lg = _saveRoots(list);
			var r = pu.Except(lg);
			foreach (var d in r) File.Delete(d);
		}
		static IEnumerable<string> _saveRoots(IEnumerable<TotalRiskFundNode> list) {
			var log = new List<string>();
			if (!list.Any()) return log;
			var serializer = new XmlSerializer(typeof(SerializableNodeMap<CushionNode>));
			foreach (var r in list) {
				var sri = (r as CommonNode).ToSerializableNodeMap(a => a.ToSerialCushion());
				var curPath = _path + r.CurrentDate.Year.ToString() + Path.DirectorySeparatorChar + r.CurrentDate.ToString("yyyy-MM-dd") + ".xml";
				using (FileStream fs = new FileStream(curPath, FileMode.Create)) {
					serializer.Serialize(fs,sri);
					log.Add(curPath);
				}
			}
			return log;
		}
		static IEnumerable<FileInfo> pickUp(DateTime since, DateTime until) {
			var d = new DirectoryInfo(_path);
			if (!d.Exists) return Enumerable.Empty<FileInfo>(); //new Dictionary<DateTime, FileInfo>();
			try {
				return d.GetDirectories("*", SearchOption.AllDirectories)
					.Where(a => ResultWithValue.Of<int>(int.TryParse, a.Name))
					.SelectMany(a => a.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
					.Select(a => new { rwv = ResultWithValue.Of<DateTime>(DateTime.TryParse, a.Name.Replace(a.Extension, string.Empty)), rsult = a })
					.Where(a => a.rwv && since <= a.rwv.Value && a.rwv.Value <= until)
					.Select(a => a.rsult);
			} catch {
				return Enumerable.Empty<FileInfo>();
			}
		}
		internal static IEnumerable<TotalRiskFundNode> ReadRoots() {
			var d = new DirectoryInfo(_path);
			if (!d.Exists) d.Create();
			try {
				var dd = d.GetDirectories("*", SearchOption.AllDirectories)
					.Where(a => ResultWithValue.Of<int>(int.TryParse, a.Name))
					.SelectMany(a => a.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
					.Where(a => ResultWithValue.Of<DateTime>(DateTime.TryParse, a.Name.Replace(a.Extension,string.Empty)))
					.Select(a => readRoot(a.FullName))
					.Where(a => a != null);
				return dd;
			} catch {
				return Enumerable.Empty<TotalRiskFundNode>();
			}
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
			return nodes?.AssembleTree(a => a.ToInstance())
				.RemoveDescendant(a => (a as AnonymousNode) != null) as TotalRiskFundNode;
		}

	}
}
