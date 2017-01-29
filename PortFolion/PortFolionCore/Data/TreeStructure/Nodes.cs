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
		string _name;
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
	/// <summary>User,ブローカーまたはアカウントのベースクラス</summary>
	public abstract class FinancialBasket : CommonNode {

		internal FinancialBasket() { init(); }
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
		public AccountNode() { }
		internal AccountNode(CushionNode cushion) : base(cushion) {
			Account = cushion.Account;
		}
		public AccountClass Account { get; set; } = AccountClass.General;
		protected override CommonNode Clone(CommonNode nd) {
			(nd as AccountNode).Account = Account;
			return base.Clone(nd);
		}
		public override CommonNode Clone() {
			return this.Clone(new AccountNode());
		}
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Account = Account;
			obj.Node = NodeType.Account;
			return obj;
		}
	}
	/// <summary>リスクファンド</summary>
	public class BrokerNode: FinancialBasket {
		public BrokerNode() { }
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
			base.SetInvestmentReturnValue(value);
		}
		public override long InvestmentReturnValue {
			get { return ChildNodes.Any() ? ChildNodes.Sum(a => a.InvestmentReturnValue) : base.InvestmentReturnValue; }
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
		internal TotalRiskFundNode() { }
		internal TotalRiskFundNode(CushionNode cushion):base(cushion) {
			CurrentDate = cushion.Date;
		}
		public override CommonNode Clone() {
			return Clone(new TotalRiskFundNode());
		}
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
		internal override CushionNode ToSerialCushion() {
			var obj = base.ToSerialCushion();
			obj.Date = _currentDate;
			obj.Node = NodeType.Total;
			return obj;
		}
	}

}
