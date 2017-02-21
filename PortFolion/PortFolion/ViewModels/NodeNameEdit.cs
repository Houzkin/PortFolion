using Houzkin.Architecture;
using Houzkin.Tree;
using Livet.Commands;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PortFolion.ViewModels {
		
	public class NodeNameEditerVM : DynamicViewModel<CommonNode> {
		
		/// <summary>新規追加または編集用ViewModel</summary>
		/// <param name="parent">親ノード</param>
		/// <param name="model">編集対象となる子ノード</param>
		public NodeNameEditerVM(CommonNode parent,CommonNode model) : base(model) {
			if (parent == null || model == null) throw new ArgumentNullException();
			Parent = parent;
			this.Name = model.Name;
		}
		public CommonNode Parent { get; private set; }
		string _name;
		public string Name {
			get { return _name; }
			set {
				SetProperty(ref _name, value, NameValidate);
				execute.RaiseCanExecuteChanged();
			}
		}
		protected virtual string NameValidate(string newName) {
			if (Parent != Model.Parent && Parent.Children.Any(a => a.Name == newName)) return "名前が重複するため追加できません";
			if(Parent == Model.Parent) {
				if (Parent.Children.Where(a => a != Model).Any(a => a.Name == newName)) return "名前が重複するため変更できません";

				var his = RootCollection.GetNodeLine(Parent.Path)
					.ToDictionary(
						a => (a.Root() as TotalRiskFundNode).CurrentDate,
						b => b);
				Func<KeyValuePair<DateTime, CommonNode>, bool> fun = 
					a => a.Value.Children
						.Where(b => b.Name != Model.Name)//名前の変更がない場合のエラー回避
						.Any(c => c.Name == newName);

				if (his.Any(fun)) {
					DateTime since = his.First(fun).Key;
					DateTime until = his.Last(fun).Key;
					if (since == until)
						return since.ToString("yyyy/MM/dd") + " において名前が重複するため変更できません";
					else
						return since.ToString("yyyy/MM/dd") + " から " + until.ToString("yyyy/MM/dd") + " の期間において名前が重複するため変更できません";
				}
			}
			return null;
		}
		void AddExecute() {
			Model.Name = Name;
			Parent.AddChild(Model);
		}
		void EditExecute() {
			var his = RootCollection.GetNodeLine(Parent.Path);
			if(his.Any(a=>a.Children.Any(b=>b.Name == this.Name))) {
				string msg = "[" + this.Name + "] は別の時系列に既に存在します。\n["
					+Model.Name+ "] は変更後、既存の ["+this.Name+"] と同一のものとして扱われます。\nこの操作は不可逆です。";
				var r = MessageBox.Show(msg, "caption", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
				if (r == MessageBoxResult.Cancel) return;
			}
			foreach(var n in RootCollection.GetNodeLine(Model.Path)) {
				n.Name = this.Name;
			}
		}
		protected virtual void ExecuteFunc() {
			if(Parent != Model.Parent) {
				AddExecute();
			}else {
				EditExecute();
			}
		}
		protected virtual bool CanExecuteFunc() {
			return !HasErrors && Model.Name != this.Name && !string.IsNullOrEmpty(this.Name) && !string.IsNullOrWhiteSpace(this.Name);
		}
		ViewModelCommand execute;
		public ViewModelCommand ExecuteCmd
			=> execute = execute ?? new ViewModelCommand(ExecuteFunc, CanExecuteFunc);
	}

	//public class NodeTagEditerVM : DynamicViewModel<CommonNode> {
	//	public NodeTagEditerVM(CommonNode model) : base(model) {
	//		TagList = RootCollection.Instance.SelectMany(a => a.Levelorder()).Select(a => a.Tag.TagName).Distinct();
	//	}
	//	public IEnumerable<string> TagList { get; private set; }
	//}
	//public class TagVM : DynamicViewModel<TagInfo> {
	//	public TagVM(TagInfo tag) : base(tag) {

	//	}
	//	string name;
	//	public string Name {
	//		get { return name; }
	//		set { name = value;
	//			this.OnPropertyChanged();
	//		}
	//	}
	//	public void ExecuteChange() {

	//	}
	//}
	
}
