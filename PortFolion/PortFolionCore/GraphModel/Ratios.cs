using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PortFolion.Core {
	public class RateData : INotifyPropertyChanged {
		internal RateData(TransitionData transition) {
			_data = transition;
			_data.PropertyChanged += transitionPropertyChanged;
			initialPoint = (_data.TimeAxis.Any()) ? _data.TimeAxis.First() : DateTime.Today;
		}
		TransitionData _data;
		public Period TimePeriod => _data.TimePeriod;
		public DividePattern Divide => _data.Divide;
		public int TargetLevel => _data.TargetLevel;
		public Ratio Ratio => _data.Ratio;

		DateTime initialPoint;
		public DateTime InitialPoint {
			get { return initialPoint; }
			set {
				if (initialPoint == value) return;
				initialPoint = value;
				OnPropertyChanged(nameof(InitialPoint));
			}
		}

		private void transitionPropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(TimePeriod):
			case nameof(Divide):
			case nameof(TargetLevel):
			case nameof(Ratio):
				OnPropertyChanged(nameof(Elements));
				break;
			}
		}
		//仮
		public Dictionary<string,IEnumerable<double>> Elements {
			get {
				return _data.CurrentMap
					.ToLookup(a => a.Tag.TagName)
					.ToDictionary(
						a => a.Key, 
						b => b.Select(
							c => (double)c.Node.Amount));
			}
		}


		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string name = "") {
			var e = new PropertyChangedEventArgs(name);
			PropertyChanged?.Invoke(this, e);
		}
	}
}
