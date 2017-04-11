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
	class LocationNode : ReadOnlyBindableTreeNode<CommonNode,LocationNode> {
		public LocationNode(CommonNode model) : base(model) {
		}

		protected override LocationNode GenerateChild(CommonNode modelChildNode) {
			throw new NotImplementedException();
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
				Selected(this, new LocationSelectedEventArgs(this.Model));
			}
		}
		public event EventHandler<LocationSelectedEventArgs> Selected;
	}
}
