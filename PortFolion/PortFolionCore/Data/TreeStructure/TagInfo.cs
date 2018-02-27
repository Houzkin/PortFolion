using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Houzkin.Tree;
using System.Text;
using System.Threading.Tasks;
using PortFolion.IO;

namespace PortFolion.Core {
    /// <summary>タグ編集オプション</summary>
    public enum TagEditParam {
        /// <summary>現在のノード以降</summary>
        FromCurrent,
        /// <summary>現在のノードから連続するポジション</summary>
        Position,
        /// <summary>現在のパスにおいて該当する全てのノード</summary>
        AllHistory,
    }
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
		//public void EditTagName(string newTagName) {
		//	EditTagName(this.TagName, newTagName);
		//}
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
		//public static bool EditTagName(string oldName,string newName) {
		//	var tgt = tagList.FirstOrDefault(a => a.TagName == oldName);
		//	if (tgt == null) return false;
		//	if (tagList.Any(a => a.TagName == newName)) return false;
		//	tgt.TagName = newName;
		//	return true;
		//}
		/// <summary>タグ名を変更して出力する</summary>
		/// <param name="dictionary">変更する対象をkey、新しいタグ名をvalueとするDictionary</param>
        public static void EditTagName(IDictionary<TagInfo,string> dictionary) {
            foreach (var dic in dictionary)
                dic.Key.TagName = dic.Value;
			
			var roots = RootCollection.Instance.Where(a => {
				var nodes = a.Preorder().Select(b => b.Tag).Distinct();
				foreach (var d in dictionary.Keys)
					if (nodes.Contains(d))
						return true;
				return false;
			});
			HistoryIO.SaveRoots(roots);
        }
        /// <summary>各ノードに対するタグを付け替える</summary>
        /// <param name="node">起点となるノード</param>
        /// <param name="tag">タグ</param>
        /// <param name="option">変更オプション</param>
        /// <returns>変更された日付</returns>
        public static IEnumerable<DateTime> Apply(CommonNode node,TagInfo tag, TagEditParam option) {
            if (node == null) throw new ArgumentNullException("node");
            if (tag == null) throw new ArgumentNullException("tag");

            var lst = new List<DateTime>();
            var root = (node.Root() as TotalRiskFundNode);
            if(root == null) {
                node.Tag = tag;
                return lst;
            } else {
                switch (option) {
                case TagEditParam.FromCurrent:
					var dc = RootCollection.GetNodeLine(node.Path, root.CurrentDate);
					foreach(var d in dc.SkipWhile(p=>p.Key < root.CurrentDate)){
						d.Value.Tag = tag;
						lst.Add(d.Key);
					}
                    break;
                case TagEditParam.Position:
                    var dd = RootCollection.GetNodeLine(node.Path, root.CurrentDate);
                    foreach(var d in dd) {
                        d.Value.Tag = tag;
                        lst.Add(d.Key);
                    }
                    break;
                case TagEditParam.AllHistory:
                    var ddd = RootCollection.GetNodeLine(node.Path);
                    foreach(var d in ddd) {
                        d.Value.Tag = tag;
                        lst.Add(d.Key);
                    }
                    break;
                }
                return lst;
            }
        }
	}

}
