using Livet.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace PortFolion.ViewModels {

	public class MenuItemVm {
		public Image Icon { get; set; }
		public string Header { get; set; }
		public MenuItemVm() : this(() => { }) { }
		public MenuItemVm(ICommand command) {
			MenuCommand = command;
		}
		public MenuItemVm(Action execute) : this(execute, () => true) { }
		public MenuItemVm(Action execute, Func<bool> canExecute) {
			MenuCommand = new ViewModelCommand(execute, canExecute);
		}
		public ICommand MenuCommand { get; protected set; }
		ObservableCollection<MenuItemVm> children;

		public ObservableCollection<MenuItemVm> Children
			=> children = children ?? new ObservableCollection<MenuItemVm>();
	}
}
