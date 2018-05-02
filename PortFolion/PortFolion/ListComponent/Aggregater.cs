using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using Livet;
using Reactive.Bindings;

namespace PortFolion.ViewModels {
	public enum ElementDisplayType{
		Hierarchy,
		LocationTree,
		Position,
	}
	public class HierarchyRoot: ObservableCollection<HierarchyElement>{
		public ElementDisplayType ListType{ get; set; }
		public TotalRiskFundNode Root{ get;private set; }
		public HierarchyRoot(TotalRiskFundNode root,ElementDisplayType edt){
			this.ResetInstance(root, edt);
		}
		public void ResetInstance(TotalRiskFundNode root,ElementDisplayType edt){
			if(Root != root || ListType != edt){
				Root = root;
				ListType = edt;
				this.Clear();
				if (Root == null) return;
				switch(ListType){
				case ElementDisplayType.Hierarchy:
					_setHierarchy();
					break;
				case ElementDisplayType.LocationTree:
					_setTree();
					break;
				case ElementDisplayType.Position:
					_setPositions();
					break;
				}
			}
		}
		void _setPositions(){
			Root.Preorder().OfType<FinancialValue>()//.MargePosition
				.Select(a => new HierarchyElement(a)).ForEach(a=>Items.Add(a));
		}
		void _setHierarchy(){
			Root.Levelorder()//position　level　のみマージ or not?
				.GroupBy(a => a.NodeIndex().CurrentDepth)
				.OrderBy(a => a.Key)
				.ForEach((a, i) => {
					this.Add(new HierarchyBucket(a) { Name = $"{i}階層({a.Count()}" });
				});
		}
		void _setTree(){
			
		}
	}
	
	public class HierarchyElement : ViewModel {
		public HierarchyElement(CommonNode node):base(){ }
		public HierarchyElement(){ }
		protected HierarchyElement(IEnumerable<CommonNode> nodes){ }
	}
	public class HierarchyBucket : HierarchyElement {
		public HierarchyBucket(IEnumerable<CommonNode> nodes) : base(nodes) {
		}
		public ReactiveProperty<bool> IsExpand { get; private set; } = new ReactiveProperty<bool>();
		public string Name{ get; set; }
		public ObservableCollection<HierarchyElement> Children{ get; }
	}
	public class HierarchyLeaf : HierarchyElement{
		public HierarchyLeaf(CommonNode node):this(new CommonNode[] { node }) { }
		public HierarchyLeaf(IEnumerable<CommonNode>nodes){ }
		public IHierarchyNode Core{ get; }
		public IEnumerable<IHierarchyNode> History{ get; }
	}
	public class HierarchyNode : HierarchyLeaf{
		public HierarchyNode(CommonNode node) :this(new CommonNode[] { node }) { }
		public HierarchyNode(IEnumerable<CommonNode> nodes):base(nodes){ }
		public bool IsExpand{ get; set; }
		public ObservableCollection<HierarchyElement> Children{ get; }
	}
}
