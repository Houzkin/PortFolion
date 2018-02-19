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
using System.Windows.Input;

namespace PortFolion.ViewModels {
    //public interface IHistoryEditer {
    //    HashSet<DateTime> EdittingList { get; }
    //    ICommand CancelCmd { get; }
    //    ICommand ExecuteCmd { get; }
    //}
	public class FromAccountEditerNameEditVM : NodeNameEditerVM {
		AccountEditVM acc;
		public FromAccountEditerNameEditVM(AccountEditVM account, CommonNode model)
			: base(account.Model, model) {
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

		public override HashSet<DateTime> EdittingList => acc.EdittingList;
	}
		
	public class NodeNameEditerVM : DynamicViewModel<CommonNode> {
		
		/// <summary>新規追加または編集用ViewModel</summary>
		/// <param name="parent">親ノード</param>
		/// <param name="model">編集対象となる子ノード</param>
		public NodeNameEditerVM(CommonNode parent,CommonNode model) : base(model) {
			if (parent == null || model == null) throw new ArgumentNullException("えらー!");
			Parent = parent;
			this._name = model.Name;
			this.PresentName = model.Name;
		}
		string title = "追加または変更";
		public string Title {
			get { return title; }
		}
		
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
			if (Parent != null && Parent != Model.Parent && Parent.Children.Any(a => a.Name == newName))
				return "名前が重複するため追加できません";
			if(Parent != null && Parent == Model.Parent) {
				if (Parent.Children.Where(a => a != Model).Any(a => a.Name == newName))
					return "名前が重複するため変更できません";
				else
					return NameValidateHistory(newName);
			}
			return null;
		}
		protected string NameValidateHistory(string newName) {
			if (Model.Name == newName) return null;
			if(Parent == Model.Parent) {
				var r = RootCollection.CanChangeNodeName(Model.Path, newName);
				if (!r) {
					DateTime since = r.Value.First();
					DateTime until = r.Value.Last();
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
			var d = Model.Upstream().OfType<TotalRiskFundNode>().LastOrDefault()?.CurrentDate;
			if (d != null) EdittingList.Add((DateTime)d);
			Messenger.Raise(new InteractionMessage("EditEndNodeName"));
		}
		void EditExecute() {
			var name = this.Name.Trim();
			//変更先のノード名で検索
			var hst = RootCollection.GetNodeLine(new NodePath<string>(Parent.Path.Concat(new string[] { name })));
			var s = new NodePath<string>(Parent.Path.Concat(new string[] { this.PresentName }));
			//現在(変更前)のノード名で検索
			var hso = RootCollection.GetNodeLine(s);
			//重複する日付
			var isc = hst.Keys.Intersect(hso.Keys);

			if (isc.Any()) {
				string msg = "重複があるため変更できません";
				MessageBox.Show(msg, "Caption", MessageBoxButton.OK);
				return;
			}
			if (hst.Any()) {
				string msg = "[" + name + "] は別の時系列に既に存在します。\n["
					+ Model.Name + "] は変更後、既存の [" + name + "] と同一のものとして扱われます。";
				var r = MessageBox.Show(msg, "caption", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
				switch (r) {
				case MessageBoxResult.Cancel:
				case MessageBoxResult.No:
				case MessageBoxResult.None:
					return;
				}
			}
			var d = RootCollection.ChangeNodeName(s, name);
			foreach (var dd in d) EdittingList.Add(dd);
			Messenger.Raise(new InteractionMessage("EditEndNodeName"));
		}
		protected virtual void ExecuteFunc() {
			if(Parent != Model.Parent) {
				AddExecute();
			}else {
				EditExecute();
			}
		}
		protected bool CanExecuteFunc() {
			var name = this.Name.Trim();
			return !HasErrors && Model.Name != name && !string.IsNullOrEmpty(name);
		}
		HashSet<DateTime> _edit;
		public virtual HashSet<DateTime> EdittingList {
			get {
				if (_edit == null) _edit = new HashSet<DateTime>();
				return _edit;
			}
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
