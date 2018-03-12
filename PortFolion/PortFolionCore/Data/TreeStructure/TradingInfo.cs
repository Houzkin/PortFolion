using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using Houzkin.Tree.Serialization;

namespace PortFolion.Core {
	//public abstract class ReadOnlyTreeNode<TModel, TViewModel> : IReadOnlyTreeNode<TViewModel>
	//where TViewModel : ReadOnlyTreeNode<TModel, TViewModel>
	//where TModel : IReadOnlyObservableTreeNode<TModel> {

	//	protected abstract TViewModel GenerateChild(TModel modelChildNode);

	//	public TViewModel Parent => throw new NotImplementedException();

	//	public IReadOnlyList<TViewModel> Children => throw new NotImplementedException();
	//}
	public class EditManager : TreeNode<EditManager> {
		public EditManager(CommonNode node){
			Instance = node;
			Dummy = node.Clone();
		}
		#region override
		protected override void InsertChildNode(int index, EditManager child) {
			base.InsertChildNode(index, child);
			this.Dummy.Children.Insert(index, child.Dummy);
		}
		protected override void SetChildNode(int index, EditManager child) {
			base.SetChildNode(index, child);
			this.Dummy.Children[index] = child.Dummy;
		}
		#endregion
		public CommonNode Instance{ get; }
		public CommonNode Dummy { get; }
		List<Func<CommonNode,IEnumerable<CommonNode>>> commands;

		public void SetCommand(Func<CommonNode,IEnumerable<CommonNode>> command){
			commands.Add(command);
			command(Dummy);
		}
		public void SetCommand(Func<CommonNode,CommonNode> command){
			this.SetCommand(f => new CommonNode[] { command(f) });
		}
		public void SetCommand(Action<CommonNode> command){
			this.SetCommand(f => { command(f); return f.Root(); });
		}
		public IEnumerable<TotalRiskFundNode> Execute(){
			var r = commands.SelectMany(a => a(Instance)).OfType<TotalRiskFundNode>().ToArray();
			commands.Clear();
			return r;
		}
		public void Cancel(){ commands.Clear(); }
		/// <summary>Instanceと比較してDummyに変更があった場合true</summary>
		public bool HasChanged{ get; }
	}
	public class BrokerEditManager {
		BrokerNode _model;
		public BrokerEditManager(BrokerNode node){
			_model = node;
			BrokerEditer = (node as CommonNode).Convert(a => new EditManager(a));

			MngList = new List<EditManager>(BrokerEditer.Levelorder());
		}
		public event Action<string> EditerMessage;
		EditManager BrokerEditer{ get; }
		public void AddAccount(AccountNode account){
			_setMng(BrokerEditer, new EditManager(account));
			BrokerEditer.SetCommand(a => {
				a.AddChild(account);//順序
			});
		}
		public void AddPosition(CommonNode parent,CommonNode node){
			var t = BrokerEditer.Preorder().FirstOrDefault(a => a.Dummy == parent);
			if (t == null) return;
			_setMng(t, new EditManager(node));
			t.SetCommand(a => {
				a.AddChild(node);//順序
			});
			
		}
		public void MovePosition(CommonNode node, CommonNode newParent){ }
		public void Remove(CommonNode node){ }
		public void Edit(CommonNode node){ }

		private void _setMng(EditManager parent, EditManager em){
			MngList.Add(em);
			parent.AddChild(em);
		}
		List<EditManager> MngList;

		public void Execute(){
			
		}

	}
}
