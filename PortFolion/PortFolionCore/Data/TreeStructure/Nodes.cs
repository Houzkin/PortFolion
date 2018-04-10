using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortFolion.IO;

namespace PortFolion.Core {
	/// <summary>共通ノード</summary>
	public abstract class CommonNode : ObservableTreeNode<CommonNode>, INotifyPropertyChanged {
		internal CommonNode() { }
		internal CommonNode(CushionNode cushion) {
			_name = cushion.Name;
			_tag = TagInfo.GetWithAdd(cushion.Tag);
			_investmentValue = cushion.InvestmentValue;
			//_investmentReturnValue = cushion.InvestmentReturnValue;
		}
		public event PropertyChangedEventHandler PropertyChanged;
		protected void RaisePropertyChanged([CallerMemberName] string name=null) {
			if (string.IsNullOrEmpty(name)) return;
			var arg = new PropertyChangedEventArgs(name);
			PropertyChanged?.Invoke(this, arg);
		}
		public NodePath<string> Path {
			get { return this.NodePath(a => a.Name); }
		}
		protected override bool CanAddChild(CommonNode child) {
			if (this.ChildNodes.Any(a => a.Name == child.Name)) return false;
			return base.CanAddChild(child);
		}
		string _name = "";
		public string Name {
			get { return _name; }
			set {
				if (_name == value) return;
				_name = value;
				RaisePropertyChanged();
			}
		}
		public bool CanChangeName(string name) {
			return this.Siblings().All(a => a.Name != name);
			//return RootCollection.CanChangeNodeName(this.Path, name);
		}
		public void ChangeName(string name) {
			if (CanChangeName(name)) this.Name = name;
			//RootCollection.ChangeNodeName(Path, name);
		}
		TagInfo _tag;
		public TagInfo Tag {
			get { return _tag ?? TagInfo.GetDefault(); }
			set {
				if (_tag == value) return;
				_tag = value;
				RaisePropertyChanged();
			}
		}
		
		long _investmentValue;
		/// <summary>投資</summary>
		public virtual bool SetInvestmentValue(long value) {
			if (_investmentValue == value) return false;
			_investmentValue = value;
			RaisePropertyChanged(nameof(InvestmentValue));
			return true;
		}
		/// <summary>投資(入金)額</summary>
		public virtual long InvestmentValue {
			get { return _investmentValue; }
		}
		
		public abstract long Amount { get; }
		/// <summary>リスク資産としてのポジションを持つかどうか示す値を取得する。</summary>
		public virtual bool HasPosition => this.Preorder().Any(
			cn => {
				if (cn.Amount != 0) return true;
				var c = cn as FinancialProduct;
				if (c != null && c.Quantity != 0) return true;
				return false;
			});
		/// <summary>取引情報を持つかどうかを示す値を取得する。</summary>
		public virtual bool HasTrading => this.Preorder().Any(
			cn => {
				if (cn.InvestmentValue != 0) return true;
				var c = cn as FinancialProduct;
				if (c != null && c.TradeQuantity != 0) return true;
				return false;
			});
		protected virtual CommonNode Clone(CommonNode node){
			node._name = _name;
			node._tag = _tag;
			return node;
		}
		public abstract CommonNode Clone();
		internal virtual CushionNode ToSerialCushion() {
			return new CushionNode() {
				Name = _name,
				Tag = Tag.TagName,
				InvestmentValue = _investmentValue,
				Node = this.GetNodeType(),
			};
		}
		public abstract NodeType GetNodeType();
	}
	public class AnonymousNode : CommonNode {
		CushionNode _cushion;
		public AnonymousNode() : this(new IO.CushionNode()){ }
		internal AnonymousNode(CushionNode cushion) {
			_cushion = cushion;
		}
		internal override CushionNode ToSerialCushion() {
			return _cushion;
		}
		public override long Amount => _cushion.Amount;

		public override CommonNode Clone() {
			throw new NotImplementedException();
		}
		public override NodeType GetNodeType() {
			return NodeType.Unknown;
		}
	}
	/// <summary>User,ブローカーまたはアカウントのベースクラス</summary>
	public abstract class FinancialBasket : CommonNode {

		internal FinancialBasket():base() {
			init();
		}
		internal FinancialBasket(CushionNode cushion) : base(cushion) {
			init();
			_amount = cushion.Amount;
		}
		void init() {
			this.StructureChanged += (s, e) => {
				if (!e.DescendantsChanged) return;
				var ds = e.DescendantInfo;
				if(ds.PreviousParentOfTarget == this && ds.Target.Parent != this) {
					ds.Target.PropertyChanged -= ChildrenPropertyChanged;
					setAmount();
				}else if(ds.PreviousParentOfTarget != this && ds.Target.Parent == this) {
					ds.Target.PropertyChanged += ChildrenPropertyChanged;
					setAmount();
				}
			};
		}

		protected virtual void ChildrenPropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(Amount)) setAmount();
		}

		long _amount;
		void setAmount() {
			_amount = this.ChildNodes.Sum(a => a.Amount);
			RaisePropertyChanged(nameof(Amount));
		}
		public override long Amount {
			get { return _amount; }
		}
		
		protected override CommonNode Clone(CommonNode node) {
			(node as FinancialBasket)._amount = _amount;
			return base.Clone(node);
		}
		public void SortChildren(){
			this.ChildNodes.Sort((a,b) => {
				var nt = a.GetNodeType() - b.GetNodeType();
				if (nt != 0) return nt;
				string toStr(CommonNode n) {
					if (n is StockValue nn) return nn.Code.ToString() + nn.Name;
					return n.Name;
				}
				var ci = System.Globalization.CultureInfo.CurrentCulture.CompareInfo;
				return ci.Compare(toStr(a),toStr(b));
			});
		}

	}
	public enum AccountClass {
		None,
		General,
		Credit,
		FX,
	}
	/// <summary>アカウント</summary>
	public class AccountNode : FinancialBasket {
		public AccountNode(AccountClass type) { Account = type; }
		public AccountNode(AccountClass type,int leva) : this(type) {
			levarage = leva;
		}
		internal AccountNode(CushionNode cushion) : base(cushion) {
			Account = cushion.Account;
			levarage = cushion.Levarage;
		}
		public AccountClass Account { get; private set; }
		int levarage = 1;
        /// <summary>口座に掛かるレバレッジ</summary>
		public int Levarage {
			get { return levarage; }
			set {
				if (levarage == value) return;
				levarage = value;
				RaisePropertyChanged();
			}
		}
		//public FinancialValue GetOrCreateNuetral() {
		//	var nd = ChildNodes.SingleOrDefault(a => a.GetNodeType() == NodeType.Cash) as FinancialValue;//.SingleOrDefault(a => a.GetType() == typeof(FinancialValue)) as FinancialValue;
		//	if (nd != null) return nd;
			
		//	var n = new FinancialValue() { Name = "余力" };
		//	this.AddChild(n);
		//	return n;
		//}
		public override long InvestmentValue
			=> ChildNodes.Sum(a => a.InvestmentValue);
			//=> GetOrCreateNuetral().InvestmentValue;
		public override bool SetInvestmentValue(long value) {
			throw new NotSupportedException();
			//var ntr = GetOrCreateNuetral();
			//ntr.SetInvestmentValue(value);
		}
		protected override void ChildrenPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.ChildrenPropertyChanged(sender, e);
			if(e.PropertyName == nameof(InvestmentValue)) {
				RaisePropertyChanged(nameof(InvestmentValue));
			}
		}
		protected override CommonNode Clone(CommonNode nd) {
			var n = nd as AccountNode;
			n.Account = Account;
			n.Levarage = Levarage;
			return base.Clone(nd);
		}
		public override CommonNode Clone() {
			return this.Clone(new AccountNode(Account));
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Account = Account;
			obj.Levarage = Levarage;
			return obj;
		}
		public override NodeType GetNodeType() {
			return NodeType.Account;
		}
	}
	/// <summary>ブローカー</summary>
	public class BrokerNode: FinancialBasket {
		public BrokerNode() : base() { }
		internal BrokerNode(CushionNode cushion) : base(cushion) { }

		public FinancialValue GetOrCreateNuetral(){
			var nd = ChildNodes.SingleOrDefault(a => a.GetNodeType() == NodeType.Cash) as FinancialValue;
			if (nd != null)
				return nd;
			var n = new FinancialValue() { Name = "余力" };
			this.AddChild(n);
			return n;
		}
		

		protected override void ChildrenPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.ChildrenPropertyChanged(sender, e);
			if (e.PropertyName == nameof(InvestmentValue) && sender.GetType()==typeof(FinancialValue))
				RaisePropertyChanged(nameof(InvestmentValue));
		}
		public override bool SetInvestmentValue(long value){
			return GetOrCreateNuetral().SetInvestmentValue(value);
		}
		//{
		//	throw new NotSupportedException();
		//}
		public override long InvestmentValue
			=> GetOrCreateNuetral().InvestmentValue;
			//get { return ChildNodes.Sum(a => a.InvestmentValue); }
			
		
		public override CommonNode Clone() {
			return Clone(new BrokerNode());
		}
		internal override CushionNode ToSerialCushion() {
			var obj =  base.ToSerialCushion();
			return obj;
		}
		public override NodeType GetNodeType() {
			return NodeType.Broker;
		}
	}
	/// <summary>ルートとなる総リスクファンド</summary>
	public class TotalRiskFundNode : BrokerNode {
		internal TotalRiskFundNode():base() { this.Name = "総リスク資産"; }
		internal TotalRiskFundNode(CushionNode cushion):base(cushion) {
			CurrentDate = cushion.Date;
		}
		public override CommonNode Clone() {
			return Clone(new TotalRiskFundNode());
		}
		/// <summary>ルートコレクションに属している間、nullでない</summary>
		internal RootCollection MainList { get; set; }
		DateTime _currentDate;
		public DateTime CurrentDate {
			get { return _currentDate; }
			set {
				TrySetCurrentDate(value);
			}
		}
		public bool TrySetCurrentDate(DateTime date) {
			DateTime dt = new DateTime(date.Year, date.Month, date.Day);
			if (MainList != null && MainList.ContainsKey(dt)) return false;
			if(dt != _currentDate) {
				_currentDate = dt;
				RaisePropertyChanged(nameof(CurrentDate));
				if (MainList != null) MainList.DateTimeChange(_currentDate);
			}
			return true;
		}
		/// <summary>
		/// 指定したパスのノード、存在しなかった場合はその祖先にあたるノードを返す。
		/// 一致するパスが見つからなかった場合は現在のノードを返す。
		/// </summary>
		/// <param name="path">パス</param>
		public CommonNode SearchNodeOf(IEnumerable<string> path) {
			var pp = this.Evolve(
				a => a.Children.Where(
					b => !b.Path.Except(path.Take(b.Path.Count())).Any()),
				(a, b) => a.Concat(b));
			return pp.LastOrDefault();
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Date = _currentDate;
			return obj;
		}
		public override NodeType GetNodeType() {
			return NodeType.Total;
		}
	}

}
