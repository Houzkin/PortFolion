using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortFolion.Core {
	public abstract class CommonNode : ObservableTreeNode<CommonNode>, INotifyPropertyChanged {
		
		public event PropertyChangedEventHandler PropertyChanged;
		protected void RaisePropertyChanged([CallerMemberName] string name=null) {
			if (string.IsNullOrEmpty(name)) return;
			var arg = new PropertyChangedEventArgs(name);
			PropertyChanged?.Invoke(this, arg);
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
		TagInfo _tag;
		public TagInfo Tag {
			get { return _tag; }
			set {
				if (_tag == value) return;
				_tag = value;
				RaisePropertyChanged();
			}
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
		/// <summary>投資額</summary>
		public virtual long InvestmentValue {
			get { return _investmentValue; }
		}

		long _investmentReturnValue;
		/// <summary>回収</summary>
		public virtual void SetInvestmentReturnValue(long value) {
			if (_investmentReturnValue == value) return;
			_investmentReturnValue = value;
			RaisePropertyChanged(nameof(InvestmentReturnValue));
		}
		/// <summary>回収額</summary>
		public virtual long InvestmentReturnValue {
			get { return _investmentReturnValue; }
		}
		
		public abstract long Amount { get; }
	}
	/// <summary>User,ブローカーまたはアカウントのベースクラス</summary>
	public abstract class FinancialBasket : CommonNode {
		protected FinancialBasket() {
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
	}
	/// <summary>アカウント</summary>
	public class AccountNode : FinancialBasket {
		public override void SetInvestmentReturnValue(long value) {
			base.SetInvestmentReturnValue(value);
			// substruct cashe
		}
		public override void SetInvestmentValue(long value) {
			base.SetInvestmentValue(value);
			// add cashe
		}
	}
	/// <summary>リスクファンド</summary>
	public class RiskFundNode: FinancialBasket {
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
		}
		public override long InvestmentReturnValue {
			get { return ChildNodes.Sum(a => a.InvestmentReturnValue); }
		}
	}
	/// <summary>ブローカー</summary>
	public class BrokerNode : RiskFundNode { }
	/// <summary>ルートとなる総リスクファンド</summary>
	public class TotalRiskFundNode : RiskFundNode {

		public DateTime CurrentDate { get; set; }
	}

	public class FinancialValue : CommonNode {
		protected override bool CanAddChild(CommonNode child) => false;

		long _amount;
		public void SetAmount(long amount) {
			if (_amount == amount) return;
			_amount = amount;
			RaisePropertyChanged(nameof(Amount));
		}
		public override long Amount {
			get { return _amount; }
		}

	}
	/// <summary>金融商品</summary>
	public class FinancialProduct : FinancialValue {

		long _quantity;
		public void SetQuantity(long quantity) {
			if (_quantity == quantity) return;
			_quantity = quantity;
			RaisePropertyChanged(nameof(Quantity));
		}
		public long Quantity {
			get { return _quantity; }
		}
	}

}
