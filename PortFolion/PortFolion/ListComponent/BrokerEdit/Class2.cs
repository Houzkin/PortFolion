using Houzkin;
using Houzkin.Architecture;
using Houzkin.Tree;
using Livet;
using Livet.Commands;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;
using System.Collections.Specialized;
using Livet.Messaging;
using System.Data;

namespace PortFolion.ViewModels {
	public class BasketTree : ReadOnlyBindableTreeNode<CommonNode, BasketTree> {
		public BasketTree(CommonNode basket):base(basket){

		}
		protected override BasketTree GenerateChild(CommonNode modelChildNode) {
			return new BasketTree(modelChildNode);
		}
	}
	public class EditPresenter : ViewModel{
		static EditPresenter _instance;
		public static EditPresenter Instance{ get => _instance; }
		public static EditPresenter Create(TotalRiskFundNode root){
			_instance = new EditPresenter(root);
			return _instance;
		}

		EditPresenter(TotalRiskFundNode root){
			Baskets.Add(new BasketTree(root));
			var b = root.Preorder().OfType<BrokerNode>().FirstOrDefault();
			if (b != null) EditTree.Add(new BrokerEditVm(b));
		}

		public ObservableCollection<BasketTree> Baskets { get; } = new ObservableCollection<BasketTree>();
		public ObservableCollection<CommonEditVm> EditTree { get; } = new ObservableCollection<CommonEditVm>();
	}
}
