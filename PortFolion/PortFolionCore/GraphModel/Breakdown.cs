using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;

namespace PortFolion.Core {
	public class Breakdown : INotifyPropertyChanged {
		internal Breakdown(TransitionData transition) {
			_data = transition;
			_data.PropertyChanged += transitionPropertyChanged;
		}

		private void transitionPropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(_data.CurrentDate):
			case nameof(_data.CurrentNode):
			case nameof(_data.TargetLevel):
			case nameof(_data.Divide):
				OnPropertyChanged(nameof(this.BreakDownItems));
				break;
			}
		}

		TransitionData _data;
		public DividePattern Divide => _data.Divide;
		public int TargetLevel => _data.TargetLevel;

		
		public IEnumerable<CommonNode> BreakDownItems {
			get {
				var cur = _data.CurrentNode;
				if ((cur as FinancialValue) != null) return cur.Siblings();
				return _data.CurrentMap
					.ToLookup(a => a.Time)
					.LastOrDefault(a => a.Key <= _data.CurrentDate)
					.Select(a => a.Node);
			}
		}





		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string name = "") {
			var e = new PropertyChangedEventArgs(name);
			PropertyChanged?.Invoke(this, e);
		}

	}
}
