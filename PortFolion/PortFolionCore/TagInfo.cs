using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.Core {
	
	public class TagInfo : INotifyPropertyChanged {

		public event PropertyChangedEventHandler PropertyChanged;
		string _tagName;
		public string TagName {
			get { return _tagName; }
			private set {
				if (_tagName == value) return;
				_tagName = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TagName)));
			}
		}
		static ICollection<TagInfo> tagList = new HashSet<TagInfo>();
		public static TagInfo GetOrCreate(string tagName) {
			var tg = tagList.FirstOrDefault(a => a.TagName == tagName);
			if (tg != null) return tg;
			tg = new TagInfo() { TagName = tagName };
			tagList.Add(tg);
			return tg;
		}
		public static bool EditTagName(string oldName,string newName) {
			var tgt = tagList.FirstOrDefault(a => a.TagName == oldName);
			if (tgt == null) return false;
			if (tagList.Any(a => a.TagName == newName)) return false;
			tgt.TagName = newName;
			return true;
		}
		public class TagManager : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;
			private void raisePropertyChanged([CallerMemberName] string name = "") {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}

		}
	}
}
