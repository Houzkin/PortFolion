using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using Houzkin.Tree.Serialization;

namespace PortFolion.Core {
	public class EditManager : TreeNode<EditManager> {
		public EditManager(CommonNode dummy, CommonNode node){
			Instance = node;
			Dummy = dummy;
		}
		public CommonNode Instance{ get; }
		public CommonNode Dummy { get; }
		List<Action<CommonNode>> commands;

		public void SetCommand(Action<CommonNode> command){
			commands.Add(command);
			command(Dummy);
		}
		public void Execute(){
			commands.ForEach(a => a(Instance));
			commands.Clear();
		}
		public void Cancel(){ commands.Clear(); }
		/// <summary>Instanceと比較してDummyに変更があった場合true</summary>
		public bool HasChanged{ get; }
	}
	public class BrokerEditManager {
		BrokerNode _model;
		public BrokerEditManager(BrokerNode node){
			_model = node;
			//Broker = node.Root().Convert(a => a.Clone(), (a, b) => a.AddChild(b)).Levelorder().First(a=>a.Path == _model.Path) as BrokerNode;
			//map = Broker.Levelorder()
			//	.Zip(_model.Levelorder(), (b, m) => Tuple.Create(b, m))
			//	.ToDictionary(a => a.Item1, a => a.Item2);
			editDic = new Dictionary<NodeIndex, Action<CommonNode>>();
			BrokerEditer = (node as CommonNode).Convert(a => new EditManager(a.Clone(), a));
			MngList = new List<EditManager>(BrokerEditer.Levelorder());
		}
		public event Action<string> EditerMessage;
		//BrokerNode Broker{ get; }
		EditManager BrokerEditer{ get; }
		public void AddAccount(AccountNode account){
			var m = new EditManager(account.Clone(), account);
			MngList.Add(m);
			BrokerEditer.SetCommand(a => { });
		}
		public void AddPosition(CommonNode parent,CommonNode node){
			
		}
		public void MovePosition(CommonNode node, CommonNode moveTo){ }
		public void Remove(CommonNode node){ }
		public void Edit(CommonNode node){ }

		List<EditManager> MngList;
		//Dictionary<CommonNode, CommonNode> keyDmyValIns;
		Dictionary<NodeIndex, Action<CommonNode>> editDic;

		public void Execute(){
			
		}

	}
}
