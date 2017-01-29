using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using System.Windows.Input;
using Houzkin.Architecture;
using PortFolion.Core;
using System.Collections.ObjectModel;
using Livet.Commands;

namespace PortFolion.ViewModels {
	public abstract class CommonNodeVM : ReadOnlyBindableTreeNode<CommonNode, CommonNodeVM> {
		internal CommonNodeVM(CommonNode model) : base(model) {
		}

		protected override CommonNodeVM GenerateChild(CommonNode modelChildNode) {
			throw new NotImplementedException();
		}
		bool isExpand;
		public bool IsExpand {
			get { return isExpand; }
			set { this.SetProperty(ref isExpand, value); }
		}
		ICommand editTagCmd;
		public ICommand EditTagCmd
			=> editTagCmd = editTagCmd ?? new ViewModelCommand(() => { });

		ObservableCollection<MenuItemVm> menuList = new ObservableCollection<MenuItemVm>();
		public ObservableCollection<MenuItemVm> MenuList => menuList;
		#region static methods
		#endregion
	}
	public class MenuItemVm {
		public string Header { get; set; }
		public MenuItemVm() : this(() => { }) { }
		public MenuItemVm(ICommand command) {
			menuCommand = command;
		}
		public MenuItemVm(Action execute) : this(execute,()=>true) { }
		public MenuItemVm(Action execute,Func<bool> canExecute) {
			menuCommand = new ViewModelCommand(execute, canExecute);
		}
		ICommand menuCommand;
		public ICommand MenuCommand => menuCommand;
		ObservableCollection<MenuItemVm> children;

		public ObservableCollection<MenuItemVm> Children 
			=> children = children ?? new ObservableCollection<MenuItemVm>();
		
	}
	public class TotalRiskFundNodeVM : CommonNodeVM {
		public TotalRiskFundNodeVM(TotalRiskFundNode model) : base(model) {
			MenuList.Add(new MenuItemVm(AddBrokerCmd) { Header = "ブローカーを追加" });
		}
		ICommand addBrokerCommand;
		public ICommand AddBrokerCmd 
			=> addBrokerCommand = addBrokerCommand ?? new ViewModelCommand(() => { });


	}
	public class BrokerNodeVM : CommonNodeVM {
		public BrokerNodeVM(AccountNode model) : base(model) {
			var addItem = new MenuItemVm() { Header = "追加" };
			addItem.Children.Add(new MenuItemVm(AddGeneralAccount) { Header = "一般" });
			addItem.Children.Add(new MenuItemVm(AddCreditAccount) { Header = "信用" });
			addItem.Children.Add(new MenuItemVm(AddFXAccount) { Header = "為替" });
			MenuList.Add(addItem);
			MenuList.Add(new MenuItemVm(RenameCmd) { Header = "ブローカー名の変更" });
			MenuList.Add(new MenuItemVm(DeleteNodeCmd) { Header = "ブローカーを除外" });
			MenuList.Add(new MenuItemVm(EditTagCmd) { Header = "タグの編集" });
		}
		
		ICommand addGeneralAccount;
		public ICommand AddGeneralAccount
			=> addGeneralAccount = addGeneralAccount ?? new ViewModelCommand(() => { });
		ICommand addCreditAccount;
		public ICommand AddCreditAccount
			=> addCreditAccount = addCreditAccount ?? new ViewModelCommand(() => { });
		ICommand addFXAccount;
		public ICommand AddFXAccount
			=> addFXAccount = addFXAccount ?? new ViewModelCommand(() => { });

		ICommand rename;
		public ICommand RenameCmd
			=> rename = rename ?? new ViewModelCommand(() => { });

		ICommand deleteNode;
		public ICommand DeleteNodeCmd
			=> deleteNode = deleteNode ?? new ViewModelCommand(() => { });

	}
	public class AccountNodeVM : CommonNodeVM {
		public AccountNodeVM(AccountNode model) : base(model) {
			MenuItemVm addItem;
			switch (model.Account) {
			case AccountClass.General:
				addItem = new MenuItemVm(() => { }) { Header = "新規買付" };
				break;
			case AccountClass.Credit:
				addItem = new MenuItemVm(() => { }) { Header = "新規建玉" };
				break;
			case AccountClass.FX:
				addItem = new MenuItemVm(() => { }) { Header = "新規ポジション" };
				break;
			default:
				addItem = new MenuItemVm(() => { });
				break;
			}
			MenuList.Add(addItem);
			MenuList.Add(new MenuItemVm(InvestOrReturnCmd) { Header = "InvestOrRetrun" });
			MenuList.Add(new MenuItemVm(RenameCmd) { Header = "アカウント名の変更" });
			MenuList.Add(new MenuItemVm(DeleteNodeCmd) { Header = "アカウントを除外" });
			MenuList.Add(new MenuItemVm(EditTagCmd) { Header = "タグの編集" });
		}
		ICommand deleteNode;
		public ICommand DeleteNodeCmd
			=> deleteNode = deleteNode ?? new ViewModelCommand(() => { });
		ICommand rename;
		public ICommand RenameCmd
			=> rename = rename ?? new ViewModelCommand(() => { });

		ICommand investOrReturnCmd;
		public ICommand InvestOrReturnCmd
			=> investOrReturnCmd = investOrReturnCmd ?? new ViewModelCommand(() => { });
	}
}
