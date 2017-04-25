using Houzkin.Tree;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using PortFolion._Core;

namespace PortFolion.Core {
	
	internal static class Utility {

		/// <summary>指定した条件の一致を一つの区切りとしてシーケンスを分割する</summary>
		/// <typeparam name="T">型</typeparam>
		/// <param name="src">シーケンス</param>
		/// <param name="predicate">シーケンスを区切る条件</param>
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

	}
}
