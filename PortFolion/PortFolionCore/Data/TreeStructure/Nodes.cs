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
	public abstract class CommonNode : ObservableTreeNode<CommonNode>, INotifyPropertyChanged {
		internal CommonNode() { }
		internal CommonNode(CushionNode cushion) {
			_name = cushion.Name;
			_tag = TagInfo.GetWithAdd(cushion.Tag);
			_investmentValue = cushion.InvestmentValue;
			_investmentReturnValue = cushion.InvestmentReturnValue;
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
			return RootCollection.CanChangeNodeName(this.Path, name);
		}
		public void ChangeName(string name) {
			RootCollection.ChangeNodeName(Path, name);
		}
		TagInfo _tag;
		public TagInfo Tag {
			get { return _tag ?? TagInfo.GetDefault(); }
			internal set {
				if (_tag == value) return;
				_tag = value;
				RaisePropertyChanged();
			}
		}
		public void SetTag(string tagName) {
			RootCollection.ChangeNodeTag(Path, tagName);
		}
		public void RemoveTag() {
			RootCollection.RemoveNodeTag(Path);
		}
		/// <summary>投資対象となるかどうか</summary>
		public virtual bool IsInvestmentTarget { get { return true; } }

		long _investmentValue;
		/// <summary>投資</summary>
		public virtual void SetInvestmentValue(long value) {
			if (_investmentValue == value) return;
			_investmentValue = value;
			RaisePropertyChanged(nameof(InvestmentValue));
		}
		/// <summary>投資(入金)額</summary>
		public virtual long InvestmentValue {
			get { return _investmentValue; }
		}

		long _investmentReturnValue;
		/// <summary>回収額</summary>
		public virtual void SetInvestmentReturnValue(long value) {
			if (_investmentReturnValue == value) return;
			_investmentReturnValue = value;
			RaisePropertyChanged(nameof(InvestmentReturnValue));
		}
		/// <summary>回収(出金)額</summary>
		public virtual long InvestmentReturnValue {
			get { return _investmentReturnValue; }
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
				InvestmentReturnValue = _investmentReturnValue,
			};
		}
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
		internal AccountNode(CushionNode cushion) : base(cushion) {
			Account = cushion.Account;
			levarage = cushion.Levarage;
		}
		public AccountClass Account { get; private set; }
		int levarage = 1;
		public int Levarage {
			get { return levarage; }
			set {
				if (levarage == value) return;
				levarage = value;
				RaisePropertyChanged();
			}
		}
		public FinancialValue GetOrCreateNuetral() {
			var nd = ChildNodes.SingleOrDefault(a => a.GetType() == typeof(FinancialValue)) as FinancialValue;
			if (nd != null) return nd;
			string name;
			switch (Account) {
			case AccountClass.General:
				name = "現金";
				break;
			case AccountClass.Credit:
				name = "保証金現金";
				break;
			case AccountClass.FX:
				name = "有効証拠金";
				break;
			default:
				name = "余力";
				break;
			}
			var n = new FinancialValue() { Name = name };
			//this.AddChild(n);
			this.InsertChild(0, n);
			return n;
		}
		public override bool IsInvestmentTarget => true;
		public override long InvestmentValue 
			=> GetOrCreateNuetral().InvestmentValue;
		public override void SetInvestmentValue(long value) {
			var ntr = GetOrCreateNuetral();
			ntr.SetInvestmentValue(value);
		}

		public override long InvestmentReturnValue 
			=> GetOrCreateNuetral().InvestmentReturnValue;
		public override void SetInvestmentReturnValue(long value) {
			var ntr = GetOrCreateNuetral();
			ntr.SetInvestmentReturnValue(value);
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
			obj.Node = NodeType.Account;
			obj.Levarage = Levarage;
			return obj;
		}
	}
	/// <summary>ブローカー</summary>
	public class BrokerNode: FinancialBasket {
		public BrokerNode() : base() { }
		internal BrokerNode(CushionNode cushion) : base(cushion) { }

		protected override void ChildrenPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.ChildrenPropertyChanged(sender, e);
			if (e.PropertyName == nameof(InvestmentValue))
				RaisePropertyChanged(nameof(InvestmentValue));
			else if (e.PropertyName == nameof(InvestmentReturnValue))
				RaisePropertyChanged(nameof(InvestmentReturnValue));
		}
		public override bool IsInvestmentTarget {
			get { return false; }
		}
		public override void SetInvestmentValue(long value) {
			throw new NotSupportedException();
		}
		public override long InvestmentValue {
			get { return ChildNodes.Sum(a => a.InvestmentValue); }
		}
		public override void SetInvestmentReturnValue(long value) {
			throw new NotSupportedException();
			//base.SetInvestmentReturnValue(value);
		}
		public override long InvestmentReturnValue {
			get { return ChildNodes.Sum(a => a.InvestmentReturnValue); }
			//get { return ChildNodes.Any() ? ChildNodes.Sum(a => a.InvestmentReturnValue) : base.InvestmentReturnValue; }
		}
		public override CommonNode Clone() {
			return Clone(new BrokerNode());
		}
		internal override CushionNode ToSerialCushion() {
			var obj =  base.ToSerialCushion();
			obj.Node = NodeType.Broker;
			return obj;
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
		public CommonNode SearchNodeOf(IEnumerable<string> path) {
			var p = this.Levelorder()
				.Select(a => a.Path.Zip(path, (b, d) => new { b, d })
					.TakeWhile(e => e.b == e.d)
					.Select(f => f.b))
				.LastOrDefault();
			//return this.Levelorder()
			//	.FirstOrDefault(a => a.Path.SequenceEqual(p));
			return this.Evolve(
					a => a.Path.Except(p).Any() ? null : a.Children,
					(c, d) => c.Concat(d))
				.LastOrDefault();
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Date = _currentDate;
			obj.Node = NodeType.Total;
			return obj;
		}
	}

}
