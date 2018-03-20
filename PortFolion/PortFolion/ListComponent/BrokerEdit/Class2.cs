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
			if (modelChildNode is FinancialBasket m) return new BasketTree(m);
			else return null;
		}
	}
	public class EditPresenter : ViewModel{
		//public EditPresenter(BrokerNode broker){
		//	Broker = new BrokerEditVm(broker) {
		//		Messenger = this.Messenger
		//	};
		//}
		//public BrokerEditVm Broker { get; } 
		public EditPresenter(TotalRiskFundNode root){ }
	}
}
