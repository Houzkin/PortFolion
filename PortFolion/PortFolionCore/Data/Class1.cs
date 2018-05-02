using Houzkin.Tree;
using PortFolion.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.Core {
	public interface IHierarchyNode {
		double Amount{ get; set; }
		double InvestmentValue{ get; set; }
		double InvestmentTotal { get; set; }
		double InvestmentReturnValue{ get; set; }
		double InvestmentReturnTotal { get; set; }
		double ProfitLoss { get; set; }
		double UnrealizedProfitLoss { get; set; }
		double UnrealizedPLRatio { get; set; }
	}
	public interface IHierarchyLeaf : IHierarchyNode {
		double PerPrice { get; set; }
		double Quantity{ get; set; }
		double TradeQuantity{ get; set; }
		double PerBuyPriceAverage { get; set; }
	}
	public class HierarchyNode : IHierarchyNode {
		public double Amount { get; set; }
		public double InvestmentValue { get; set; }
		public double InvestmentTotal { get; set; }
		public double InvestmentReturnValue { get; set; }
		public double InvestmentReturnTotal { get; set; }
		public double ProfitLoss { get; set; }
		public double UnrealizedProfitLoss { get; set; }
		public double UnrealizedPLRatio { get; set; }
	}
	public class HierarchyLeaf : HierarchyNode, IHierarchyLeaf {
		public double PerPrice { get; set; }
		public double PerBuyPriceAverage { get; set; }
		public double Quantity { get; set; }
		public double TradeQuantity { get; set; }
	}

	public enum StructType{
		Hierarchy,Location,
	}
	public abstract class AggregatableTree : TreeNode<AggregatableTree>{
		#region static
		static IHierarchyNode ToCore(CommonNode node) {
			IHierarchyNode td;
			if (node is FinancialProduct n) {
				td = new HierarchyLeaf() {
					Quantity = n.Quantity,
					TradeQuantity = n.TradeQuantity,
					PerPrice = n.Amount / n.Quantity,
				};
			}
			else {
				td = new HierarchyNode();
			}
			td.Amount = node.Amount;
			if (0 <= node.InvestmentValue) td.InvestmentValue = node.InvestmentValue;
			else td.InvestmentReturnValue = Math.Abs(node.InvestmentValue);
			return td;
		}
		static IHierarchyNode ToCore(IEnumerable<CommonNode> nodes){
			return nodes.Select(a => ToCore(a)).Aggregate((a,b)=> _MargeCore(a,b));
		}
		static IHierarchyNode _MargeCore(IHierarchyNode a, IHierarchyNode b){
			IHierarchyNode func2(IHierarchyLeaf al,IHierarchyLeaf bl){
				al.Quantity += bl.Quantity;
				al.TradeQuantity += bl.TradeQuantity;
				al.PerPrice = al.Amount / al.Quantity;
				return al;
			}
			a.Amount += b.Amount;
			a.InvestmentReturnValue += b.InvestmentReturnValue;
			a.InvestmentValue += b.InvestmentValue;
			if (a is IHierarchyLeaf aa && b is IHierarchyLeaf bb) return func2(aa, bb);
			return a;
		}
		public static AggregatableTree Create(CommonNode node){
			return node.Convert(cn => new AggregatableElement(cn) as AggregatableTree);
		}
		public static AggregatableTree CreateMargedTree(CommonNode node){
			AggregatableElement func(CommonNode cn, Func<CommonNode, bool> prd) {
				var seq = cn.Preorder().Where(prd).Memoize();
				return seq.Any() ? new AggregatableElement(seq) : null;
			}
			var g = node.Preorder()
				.GroupBy(a => new { Name = a.Name, Type = a.GetNodeType() })
				.Select(a => new AggregatableElement(a, c => func(c, n => n.Name == a.Key.Name && n.GetNodeType() == a.Key.Type)))
				.OrderBy(a => a.Type);
			var first = new Separator("1層");
			var secon = new Separator("2層");
			var therd = new Separator("3層");
			var forth = new Separator("4層");
			foreach(var n in g){
				switch(n.Type){
				case NodeType.Total:
					first.AddChild(n);
					break;
				case NodeType.Broker:
					secon.AddChild(n);
					break;
				case NodeType.Account:
					therd.AddChild(n);
					break;
				default:
					forth.AddChild(n);
					break;
				}
			}
			var root = new Separator("root");
			return root.AddChild(first).AddChild(secon).AddChild(therd).AddChild(forth);
		}
		#endregion
		protected AggregatableTree(CommonNode node){
			Data = ToCore(node);
			Name = node.Name;
			Type = node.GetNodeType();
			CurrentDate = (node as TotalRiskFundNode).CurrentDate;
		}
		protected AggregatableTree(IEnumerable<CommonNode> nodes){
			Name = nodes.First().Name;
			Type = nodes.First().GetNodeType();
			Data = ToCore(nodes);
			CurrentDate = (nodes.First().Root() as TotalRiskFundNode).CurrentDate;
		}
		protected AggregatableTree(string name){
			Type = NodeType.Unknown;
			Name = name;
		}
		public IEnumerable<string> Path => this.Upstream().Reverse().Select(a => a.Name);
		public string Name{ get; }
		public NodeType Type { get; }
		public virtual DateTime CurrentDate { get; }
		public IHierarchyNode Data{ get; set; }
		public abstract StructType Structure{ get; }

		public void Calculate(){
			if (this.IsRoot()) {
				if (Structure == StructType.Hierarchy)
					this.Postorder().ForEach(a => a.Calc());
				else
					this.Preorder().OrderByDescending(a => a.Type).ForEach(a => a.Calc());
			}
			else this.Root().Calculate();
		}
		protected virtual void Calc(){ }
		protected virtual Dictionary<Tuple<DateTime,string>,AggregatableElement> CalcHis(Dictionary<Tuple<DateTime,string>,AggregatableElement> dic){
			return dic;
		}

		//	public static IHierarchyNode MargeToCore(this IEnumerable<CommonNode> self){
		//		var r = self.First().Root();
		//		if (self.Skip(1).Any(a => a.Root() != r)) throw new ArgumentException();
		//		return self.Select(a => a.ToCore())
		//			.Aggregate((a, b) => {
		//				a.InvestmentValue += b.InvestmentValue;
		//				a.InvestmentReturnValue += b.InvestmentReturnValue;
		//				a.Amount += b.Amount;
		//				if(a is IHierarchyLeaf aa && b is IHierarchyLeaf bb){
		//					aa.Quantity += bb.Quantity;
		//					aa.TradeQuantity += bb.TradeQuantity;
		//					aa.PerPrice = aa.Amount / aa.Quantity;
		//				}
		//				return a;
		//			});
		//	}
		//	public static IEnumerable<IHierarchyNode> CalcHistory(this IEnumerable<IHierarchyNode> history){
		//		var nd = history
		//			.Scan(new HierarchyLeaf() as IHierarchyNode,
		//				(prv, cur) => {
		//					cur.InvestmentTotal = prv.InvestmentTotal + cur.InvestmentValue;
		//					cur.InvestmentReturnTotal = prv.InvestmentReturnTotal + cur.InvestmentReturnValue;
		//						if(cur is IHierarchyLeaf curl){

		//						}else{
		//							//cur.UnrealizedProfitLoss;
		//						}
		//					return cur;
		//				});
		//		return nd;
		//	}
	}
	public class AggregatableElement : AggregatableTree{
		public AggregatableElement(CommonNode node) : base(node){
			var ndp = node.Path.ToArray();
			_createFunc = cmn => {
				var his = cmn.Evolve(
						b => b.IsRoot() ? b.Name == ndp.ElementAtOrDefault(0) ? b.Children : null : b.Children,
						(b, c) => b.Concat(c.Where(d => d.Name == ndp.ElementAtOrDefault(d.Upstream().Count() - 1))))
					.Where(b => b.Path.SequenceEqual(ndp))
					.Memoize();
				return his.Any() ? Create(his.First()) as AggregatableElement : null;
			};
			this.Structure = StructType.Hierarchy;
		}
		public AggregatableElement(IEnumerable<CommonNode> nodes):base(nodes.First()){
			this.Structure = StructType.Location;
		}
		public AggregatableElement(IEnumerable<CommonNode> nodes,Func<CommonNode,AggregatableElement> func):this(nodes){
			_createFunc = func;
		}
		Func<CommonNode, AggregatableElement> _createFunc;
		public override StructType Structure { get; }

		#region calc
		protected override void Calc() {
			if (_createFunc == null) return;
			var ins = RootCollection.Instance
				.TakeWhile(a => a.CurrentDate <= CurrentDate)
				.Select(a => _createFunc(a))
				.Where(a => a != null);

		}
		#endregion
	}
	/// <summary>計算機能のないセパレータ</summary>
	public class Separator : AggregatableTree{
		public Separator(string name) : base(name){ }
		public override StructType Structure => StructType.Location;
		public override DateTime CurrentDate 
			=> this.Preorder().OfType<AggregatableElement>().FirstOrDefault()?.CurrentDate ?? base.CurrentDate;
	}
}
