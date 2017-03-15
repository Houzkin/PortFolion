using Houzkin.Architecture;
using Houzkin.Tree;
using Livet.Commands;
using Livet.Messaging;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PortFolion.ViewModels {
	public class FromAccountEditerNameEditVM : NodeNameEditerVM {
		AccountEditVM acc;
		public FromAccountEditerNameEditVM(AccountEditVM account, CommonNode parent,CommonNode model)
			: base(parent, model) {
			acc = account;
		}
		public override InteractionMessenger Messenger => acc.Messenger;
		protected override string NameValidate(string newName) {
			newName = newName.Trim();
			if (acc.Elements.Where(a => a.Model != this.Model).Select(a => a.Name).Contains(newName))
				return "名前が重複するため変更できません";
			else
				return NameValidateHistory(newName);
		}
		protected override void ExecuteFunc() {
			if (Parent != Model.Parent) {
				Model.Name = Name.Trim();
				acc.NodeNameEditer = null;
			} else {
				base.ExecuteFunc();
			}
		}
		ViewModelCommand _cancelCmd;
		public override ViewModelCommand CancelCmd
			=> _cancelCmd = _cancelCmd ?? new ViewModelCommand(() => acc.NodeNameEditer = null);
	}
		
	public class NodeNameEditerVM : DynamicViewModel<CommonNode> {
		
		/// <summary>新規追加または編集用ViewModel</summary>
		/// <param name="parent">親ノード</param>
		/// <param name="model">編集対象となる子ノード</param>
		public NodeNameEditerVM(CommonNode parent,CommonNode model) : base(model) {
			if (parent == null || model == null) throw new ArgumentNullException();
			Parent = parent;
			this.Name = model.Name;
			this.PresentName = model.Name;
		}
		string title;
		public string Title {
			get { return title; }
			set { SetProperty(ref title, value); }
		}
		//AccountEditVM acc;
		//public NodeNameEditerVM(AccountEditVM account, CommonNode parent, CommonNode model) : this(parent, model) {
		//	acc = account;
		//}
		InteractionMessenger _messenger;
		public virtual InteractionMessenger Messenger
			=> _messenger = _messenger ?? new InteractionMessenger(); 

		public CommonNode Parent { get; private set; }
		public string PresentName { get; private set; }
		string _name;
		public string Name {
			get { return _name; }
			set {
				SetProperty(ref _name, value, NameValidate);
				this.ExecuteCmd.RaiseCanExecuteChanged();
			}
		}
		protected virtual string NameValidate(string newName) {
			newName = newName.Trim();
			if (Parent != Model.Parent && Parent.Children.Any(a => a.Name == newName)) return "名前が重複するため追加できません";
			if(Parent == Model.Parent) {
				if (Parent.Children.Where(a => a != Model).Any(a => a.Name == newName))
					return "名前が重複するため変更できません";
				else
					return NameValidateHistory(newName);
			}
			return null;
		}
		protected string NameValidateHistory(string newName) {
			if(Parent == Model.Parent) {
				var his = RootCollection.GetNodeLine(Parent.Path);
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
			Model.Name = Name.Trim();
			Parent.AddChild(Model);
			Messenger.Raise(new InteractionMessage("EditEndNodeName"));
		}
		void EditExecute() {
			var name = this.Name.Trim();
			var his = RootCollection.GetNodeLine(Parent.Path).Values;
			if(his.Any(a=>a.Children.Any(b=>b.Name == name))) {
				string msg = "[" + name + "] は別の時系列に既に存在します。\n["
					+Model.Name+ "] は変更後、既存の ["+name+"] と同一のものとして扱われます。\nこの操作は元に戻せません。";
				var r = MessageBox.Show(msg, "caption", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
				if (r == MessageBoxResult.Cancel) return;
			}
			foreach(var n in RootCollection.GetNodeLine(Model.Path).Values) {
				n.Name = name;
			}
			Messenger.Raise(new InteractionMessage("EditEndNodeName"));
		}
		protected virtual void ExecuteFunc() {
			if(Parent != Model.Parent) {
				AddExecute();
			}else {
				EditExecute();
			}
		}
		protected virtual bool CanExecuteFunc() {
			var name = this.Name.Trim();
			return !HasErrors && Model.Name != name && !string.IsNullOrEmpty(name);
		}
		ViewModelCommand execute;
		public ViewModelCommand ExecuteCmd
			=> execute = execute ?? new ViewModelCommand(ExecuteFunc, CanExecuteFunc);
		ViewModelCommand cancel;
		public virtual ViewModelCommand CancelCmd
			=> cancel = cancel ?? new ViewModelCommand(
				() => Messenger.Raise(new InteractionMessage("EditEndNodeName")));
				
		
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
