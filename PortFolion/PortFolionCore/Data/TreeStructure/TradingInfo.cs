using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;

namespace PortFolion.Core {
	public class EditManager{
		public EditManager(CommonNode node){
			CurrentName = node.Name;
			NewName = node.Name;
		}
		
		public string CurrentName{ get; }
		public string NewName{ get; }

		public void Execute(){ }
		public void Cancel(){ }
	}
	public class BrokerEditManager {
		BrokerNode _model;
		public BrokerEditManager(BrokerNode node){
			_model = node;
			Broker = node.Clone() as BrokerNode;
			editDic = new Dictionary<CommonNode, Action<CommonNode>>();
		}
		public event Action<string> EditerMessage;
		BrokerNode Broker{ get; }
		public void AddAccount(AccountNode account){
			
		}
		public void AddPosition(CommonNode parent,CommonNode node){ }
		public void MovePositon(CommonNode node, CommonNode moveTo){ }
		public void Remove(CommonNode node){ }

		Dictionary<CommonNode, Action<CommonNode>> editDic;

		public void Execute(){
			//var idx = _model.BranchIndex();
			//_model.Children[idx] = Broker;
			//foreach(var n in Broker.Levelorder()){
			//	if (editDic.TryGetValue(n, out Action<CommonNode> action)) {
			//		action(n);
			//	}
			//}
		}

	}
}
