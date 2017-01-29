using Houzkin.Architecture;
using Livet.Commands;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public AddBrokerVM(CommonNode parent) : this(parent,new BrokerNode()) { } 
		protected AddBrokerVM(CommonNode parent,CommonNode model) : base(model) {
			Parent = parent;
		}
		public CommonNode Parent { get; private set; }
		public string Name {
			get { return Model.Name; }
			set {
				Model.Name = value;
				OnPropertyChanged();
				ValidateProperty(Model.Name, NameValidate);
				execute.RaiseCanExecuteChanged();
			}
		}
		protected virtual string NameValidate(string newName) {
			if (Parent.Children.Any(a => a.Name == Name)) return "名前が重複するため追加できません";
			return null;
		}
		protected virtual void ExecuteFunc() {
			Parent.AddChild(Model);
		}
		protected virtual bool CanExecuteFunc() {
			return !HasErrors && !string.IsNullOrEmpty(Model.Name) && !string.IsNullOrWhiteSpace(Model.Name);
		}
		ViewModelCommand execute;
		public ViewModelCommand ExecuteCmd
			=> execute = execute ?? new ViewModelCommand(ExecuteFunc, CanExecuteFunc);
	}
	
	public class AddAccountVM : AddBrokerVM {
		public AddAccountVM(CommonNode parent,AccountClass type):base(parent, new AccountNode()) {
			this.AccountType = type;
			Neutral= new FinancialValue();
			Model.AddChild(Neutral);
			setNeutralValueName();
		}
		public FinancialValue Neutral { get; private set; }
		AccountClass type;
		public AccountClass AccountType { get; private set; }
		void setNeutralValueName() {
			string ac;
			switch (AccountType) {
			case AccountClass.General:
				Neutral.Name = "現金";
				ac = "総合口座";
				break;
			case AccountClass.Credit:
				Neutral.Name = "買付余力";
				ac = "信用口座";
				break;
			case AccountClass.FX:
				Neutral.Name = "有効証拠金";
				ac = "FX口座";
				break;
			default:
				Neutral.Name = "その他";
				ac = "口座";
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
