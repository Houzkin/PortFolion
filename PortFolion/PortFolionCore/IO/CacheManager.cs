using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PortFolion.IO {
	public class CacheManager {
		
		static readonly string _path = AppDomain.CurrentDomain.BaseDirectory + "cache";
		static string checkPath(string[] path) {
			if (!path.Any()) throw new ArgumentNullException();
			var p = _path + path.Aggregate("", (a, b) => a + Path.DirectorySeparatorChar + b);
			var d = Path.GetDirectoryName(p);
			if (!Directory.Exists(d)) {
				Directory.CreateDirectory(d);
			}
			return p;
		}

		public static void SaveCache(string data, params string[] path) {
			var s = checkPath(path);
			using (var sw = new StreamWriter(s,false)) {
				sw.Write(data);
			}
		}
		public static void SaveCache<T>(T data, params string[] path) {
			var seri = new XmlSerializer(typeof(T));
			using(var fs = new FileStream(checkPath(path), FileMode.Create)) {
				seri.Serialize(fs, data);
			}
		}

		public static string ReadCache(params string[] path) {
			if (!File.Exists(checkPath(path))) return "";
			using(var sr = new StreamReader(checkPath(path))) {
				return sr.ReadToEnd();
			}
		}
		public static T ReadCache<T>(params string[] path) {
			if (!File.Exists(checkPath(path))) return default(T);
			var seri = new XmlSerializer(typeof(T));
			using(var fs = new FileStream(checkPath(path), FileMode.Open)) {
				return (T)seri.Deserialize(fs);
			}
		}

		public static FileInfo GetFileInfo(params string[] path) {
			return new FileInfo(checkPath(path));
		}

		public static void Clear() {
			if (Directory.Exists(_path))
				Directory.Delete(_path, true);
		}
	}
}
