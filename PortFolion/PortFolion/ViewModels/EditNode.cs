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
	public class EditNodeNameVM : DynamicViewModel<CommonNode> {
		public EditNodeNameVM(CommonNode model) : base(model) {
			_editName = model.Name;
		}
		public string Name { get { return Model.Name; } }
		string _editName;
		public string EditName {
			get { return _editName; }
			set {
				SetProperty(ref _editName, value, nameValidation);
				execute.RaiseCanExecuteChanged();
			}
		}
		string nameValidation(string name) {
			if (Name == name) return "";
			return this.Model.CanChangeName(name) ? "" : "現在または過去の入力データにおいて名前が重複するため変更できません";
		}
		ViewModelCommand execute;
		public ViewModelCommand ExecuteCmd
			=> execute = execute ?? new ViewModelCommand(() => Model.ChangeName(EditName), () => !this.HasErrors　&& Name != EditName);
	}
	public class AddBrokerVM : DynamicViewModel<CommonNode> {

		public AddBrokerVM(CommonNode riskFundNode) : this(riskFundNode,new BrokerNode()) { } 
		protected AddBrokerVM(CommonNode parent,CommonNode model) : base(model) {
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
				
				var his = RootCollection.GetNodeLine(Parent.Path).ToDictionary(a=>(a.Root() as TotalRiskFundNode).CurrentDate,b=>b);
				Func<KeyValuePair<DateTime, CommonNode>, bool> fun = a
					=> a.Value.Children
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
		protected virtual void ExecuteFunc() {
			if(Parent != Model.Parent) {
				Model.Name = _name;
				Parent.AddChild(Model);
			}else {
				var his = RootCollection.GetNodeLine(Parent.Path);
				if(his.Any(a=>a.Children.Any(b=>b.Name == this.Name))) {
					string msg = "[" + this.Name + "] は別の時系列に既に存在します。\n["
						+Model.Name+ "] は変更後、既存の ["+this.Name+"] と同一のものとして扱われます。\nこの操作は不可逆です。";
					var r = MessageBox.Show(msg, "caption", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
					if (r == MessageBoxResult.Cancel) return;
				}
				foreach(var n in RootCollection.GetNodeLine(Model.Path)) {
					n.Name = _name;
				}
			}
		}
		protected virtual bool CanExecuteFunc() {
			return !HasErrors && Model.Name != this.Name && !string.IsNullOrEmpty(this.Name) && !string.IsNullOrWhiteSpace(this.Name);
		}
		ViewModelCommand execute;
		public ViewModelCommand ExecuteCmd
			=> execute = execute ?? new ViewModelCommand(ExecuteFunc, CanExecuteFunc);
	}
	
	public class AddAccountVM : AddBrokerVM {
		public AddAccountVM(CommonNode parent, CommonNode child,AccountClass type) : base(parent, child) {
			AccountType = type;
		}
		public AddAccountVM(CommonNode parent,AccountClass type):base(parent, new AccountNode(type)) {
			this.AccountType = type;
			//Neutral= new FinancialValue();
			//Model.AddChild(Neutral);
			setDefaultName();
		}
		//public FinancialValue Neutral { get; private set; }
		//AccountClass type;
		public AccountClass AccountType { get; private set; }
		void setDefaultName() {
			string ac;
			switch (AccountType) {
			case AccountClass.General:
				//Neutral.Name = "現金";
				ac = "総合口座";
				break;
			case AccountClass.Credit:
				//Neutral.Name = "買付余力";
				ac = "信用口座";
				break;
			case AccountClass.FX:
				//Neutral.Name = "有効証拠金";
				ac = "FX口座";
				break;
			default:
				//Neutral.Name = "その他";
				ac = "その他";
				break;
			}
			this.Name = ac;
			int number = 1;
			while (HasErrors && number <= 10) {
				this.Name = ac + number.ToString();
				number++;
			}
			if (HasErrors) this.Name = "";
		}
		
	}
	
}
