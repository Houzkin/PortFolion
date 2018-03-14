using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using Houzkin.Tree.Serialization;

namespace PortFolion.Core {
	//駄file	

	public class EditManager :IDisposable {
		public EditManager(CommonNode node) 
			: this(node, new HashSet<CommonNode>()) { }

		public EditManager(CommonNode node,HashSet<CommonNode> hash){
			Instance = node;
			ChangedList = hash;
		}
		public CommonNode Instance{ get; }
		public HashSet<CommonNode> ChangedList { get; } 

		public void SetCommand(Func<CommonNode,IEnumerable<CommonNode>> command){
			
		}
		public void SetCommand(Func<CommonNode,CommonNode> command){
			this.SetCommand(f => new CommonNode[] { command(f) });
		}
		public void SetCommand(Action<CommonNode> command){
			this.SetCommand(f => { command(f); return f.Root(); });
		}
		public IEnumerable<TotalRiskFundNode> Execute(){
			throw new NotImplementedException();
		}
		public void Cancel(){
			RootCollection.ReRead(ChangedList.OfType<TotalRiskFundNode>());
			ChangedList.Clear();
		}
		public void Dispose(){
			Cancel();
		}
	}
	public class BrokerEditManager {
		public BrokerEditManager(CommonNode node){
			_model = node;
			MngList = new List<EditManager>();
			
			MngList.AddRange(_model.Preorder().Select(a => new EditManager(a)));
			CurrentEditer = MngList.First();
		}
		public event Action<string> EditerMessage;
		CommonNode _model;
		EditManager CurrentEditer{ get; }

		
		public void AddAccount(AccountNode account){
			_setMng(CurrentEditer, new EditManager(account));
			CurrentEditer.SetCommand(a => {
				a.AddChild(account);//順序
			});
		}
		public void AddPosition(CommonNode parent,CommonNode node){
			throw new NotImplementedException();
		}
		public void MovePosition(CommonNode node, CommonNode newParent){ }
		public void Remove(CommonNode node){ }
		public void Edit(CommonNode node){ }

		private void _setMng(EditManager parent, EditManager em){
			MngList.Add(em);
			//parent.AddChild(em);
			throw new NotImplementedException();
		}
		List<EditManager> MngList;

		public void Execute(){
			
		}

	}
}
