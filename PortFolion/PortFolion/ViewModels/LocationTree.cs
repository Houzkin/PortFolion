using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Architecture;
using PortFolion.Core;
using Houzkin.Tree;

namespace PortFolion.ViewModels {
	public class LocationSelectedEventArgs  : EventArgs {
		public LocationSelectedEventArgs (CommonNode node) {
			Location = node;
		}
		public CommonNode Location { get; private set; }
	}
	public class LocationNode : ReadOnlyBindableTreeNode<CommonNode,LocationNode> {
		public LocationNode(CommonNode model) : base(model) {
		}
		protected override LocationNode GenerateChild(CommonNode modelChildNode) {
			return new LocationNode(modelChildNode);
		}

		public bool IsModelEquals(CommonNode node) {
			return node == this.Model;
		}
		public NodePath<string> Path => Model.Path;
		bool _isExpand = false;
		public bool IsExpand {
			get { return _isExpand; }
			set { this.SetProperty(ref _isExpand, value); }
		}
		
		bool _isSelected;
		public bool IsSelected {
			get { return _isSelected; }
			set {
				if (_isSelected == value) return;
				_isSelected = value;
				OnPropertyChanged();
				if(_isSelected)
					RaiseSelected(this.Model);
			}
		}
		protected virtual void RaiseSelected(CommonNode node) {
			this.Root().RaiseSelected(node);
		}
	}
	public class LocationRoot : LocationNode {
		public LocationRoot(TotalRiskFundNode model,CommonNode cur) : base(model) {
			selectAt(cur);
		}
		void selectAt(CommonNode cur) {
			var s = this.Levelorder().FirstOrDefault(a => a.IsModelEquals(cur));
			if (s != null) s.IsSelected = true;
		}
		protected override void RaiseSelected(CommonNode node) {
			Selected?.Invoke(this, new LocationSelectedEventArgs(node));
		}
		public event EventHandler<LocationSelectedEventArgs> Selected;
	}
}
