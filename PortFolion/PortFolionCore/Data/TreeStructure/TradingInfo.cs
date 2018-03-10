using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using Houzkin.Tree.Serialization;

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
			Broker = node.Root().Convert(a => a.Clone(), (a, b) => a.AddChild(b)).Levelorder().First(a=>a.Path == _model.Path) as BrokerNode;
			map = Broker.Levelorder()
				.Zip(_model.Levelorder(), (b, m) => Tuple.Create(b, m))
				.ToDictionary(a => a.Item1, a => a.Item2);
			editDic = new Dictionary<NodeIndex, Action<CommonNode>>();
		}
		public event Action<string> EditerMessage;
		BrokerNode Broker{ get; }
		public void AddAccount(AccountNode account){
			
		}
		public void AddPosition(CommonNode parent,CommonNode node){ }
		public void MovePositon(CommonNode node, CommonNode moveTo){ }
		public void Remove(CommonNode node){ }
		public void Edit(CommonNode node){ }

		Dictionary<CommonNode, CommonNode> map;
		Dictionary<NodeIndex, Action<CommonNode>> editDic;

		public void Execute(){
			//var idx = _model.BranchIndex();
			//_model.Children[idx] = Broker;
			//foreach(var n in Broker.Levelorder()){
			//	if (editDic.TryGetValue(n, out Action<CommonNode> action)) {
			//		action(n);
			//	}
			//}
			foreach(var n in Broker.Levelorder()){
				var ni = n.NodeIndex();
				if(editDic.TryGetValue(ni, out Action<CommonNode> action)){
					
				}
			}
		}

	}
}
