using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.Core {
	
	public class TagInfo : INotifyPropertyChanged {

		public event PropertyChangedEventHandler PropertyChanged;
		private void raisePropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		string _tagName;
		public string TagName {
			get { return _tagName; }
			private set {
				if (_tagName == value) return;
				_tagName = value;
				raisePropertyChanged();
			}
		}
		public void EditTagName(string newTagName) {
			EditTagName(this.TagName, newTagName);
		}
		public bool CanEdit {
			get { return TagName != _defaultStr; }
		}
		public override string ToString() {
			return TagName;
		}
		static readonly string _defaultStr = "その他";
		static TagInfo _default = null;
		public static TagInfo GetDefault() {
			if(_default == null) {
				_default = new TagInfo() { _tagName = _defaultStr };
				//_default = GetWithAdd(_defaultStr);
			}
			return _default;
		}
		static ObservableCollection<TagInfo> tagList = new ObservableCollection<TagInfo>();
		public static ReadOnlyObservableCollection<TagInfo> GetList() {
			return new ReadOnlyObservableCollection<TagInfo>(tagList);
		}
		public static TagInfo GetOrCreate(string tagName) {
			if (tagName == _defaultStr || string.IsNullOrEmpty(tagName) || string.IsNullOrWhiteSpace(tagName))
				return GetDefault();
			var tg = tagList.FirstOrDefault(a => a.TagName == tagName);
			if (tg != null) return tg;
			return new TagInfo() { TagName = tagName };
		}
		public static TagInfo GetWithAdd(string tagName) {
			return GetWithAdd(GetOrCreate(tagName));
		}
		public static TagInfo GetWithAdd(TagInfo newTag) {
			if (!tagList.Any(a => a.TagName == newTag.TagName)) tagList.Add(newTag);
			return tagList.First(a => a.TagName == newTag.TagName);
		}
		public static bool EditTagName(string oldName,string newName) {
			var tgt = tagList.FirstOrDefault(a => a.TagName == oldName);
			if (tgt == null) return false;
			if (tagList.Any(a => a.TagName == newName)) return false;
			tgt.TagName = newName;
			return true;
		}
	}

}
