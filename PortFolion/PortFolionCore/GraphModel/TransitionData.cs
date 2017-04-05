using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using System.Runtime.CompilerServices;
using static PortFolion.Core.Utility;
using PortFolion.Core;

namespace PortFolion._Core {

	/// <summary>表示用に補正された時系列を管理する</summary>
	//public class _TransitionData : INotifyPropertyChanged {
	//	public event PropertyChangedEventHandler PropertyChanged;
	//	private void OnPropertyChanged([CallerMemberName] string name = "") {
	//		var e = new PropertyChangedEventArgs(name);
	//		PropertyChanged?.Invoke(this, e);
	//	}

	//	#region parametor
	//	public Period TimePeriod {
	//		get { return _param.TimePeriod; }
	//		set {
	//			if (_param.TimePeriod == value) return;
	//			_param.TimePeriod = value;
	//			OnPropertyChanged();
	//			setTimeAxis();
	//			setCurrentMap();
	//		}
	//	}
	//	public _DividePattern Divide {
	//		get { return _param.Divide; }
	//		set {
	//			if (_param.Divide == value) return;
	//			_param.Divide = value;
	//			OnPropertyChanged();
	//			setCurrentMap();
	//		}
	//	}
	//	public int TargetLevel {
	//		get { return _param.TargetLevel; }
	//		set {
	//			if (_param.TargetLevel == value) return;
	//			_param.TargetLevel = value;
	//			OnPropertyChanged();
	//			setCurrentMap();
	//		}
	//	}
	//	public _CalculationUnit InvestmentUnit {
	//		get { return _param.InvestmentUnit; }
	//		set {
	//			if (_param.InvestmentUnit == value) return;
	//			_param.InvestmentUnit = value;
	//			OnPropertyChanged();
	//			setCurrentMap();
	//		}
	//	}
	//	public _Ratio Ratio {
	//		get { return _param.Ratio; }
	//		set {
	//			if (_param.Ratio == value) return;
	//			_param.Ratio = value;
	//			OnPropertyChanged();
	//		}
	//	}

	//	_TransitionParametor _param = new _TransitionParametor();
	//	public _TransitionParametor GetParametor() {
	//		return new _TransitionParametor() {
	//			Divide = this.Divide,
	//			TimePeriod = this.TimePeriod,
	//			TargetLevel = this.TargetLevel,
	//			InvestmentUnit = this.InvestmentUnit,
	//			Ratio = this.Ratio,
	//		};
	//	}

	//	public void SetParametor(_TransitionParametor param) {
	//		var prv = _param;
	//		_param = param;
	//		var r = new List<Tuple<bool, Action>>() {
	//			Tuple.Create(prv.TimePeriod != this.TimePeriod,
	//				new Action(() => {
	//					setTimeAxis();
	//					OnPropertyChanged(nameof(TimePeriod));
	//				})),
	//			Tuple.Create(prv.Divide != this.Divide,
	//				new Action(() => OnPropertyChanged(nameof(Divide)))),
	//			Tuple.Create(prv.TargetLevel != this.TargetLevel,
	//				new Action(() => OnPropertyChanged(nameof(TargetLevel)))),
	//			Tuple.Create(prv.InvestmentUnit != this.InvestmentUnit,
	//				new Action(() => OnPropertyChanged(nameof(InvestmentUnit)))),
	//			Tuple.Create(prv.Ratio != this.Ratio,
	//				new Action(()=> OnPropertyChanged(nameof(Ratio)))),
	//		};
	//		if (r.Any(a => a.Item1)) { setCurrentMap(); }
	//		foreach (var rs in r) if (rs.Item1) rs.Item2();
	//	}
	//	public DateTime CurrentDate {
	//		get { return (CurrentNode.Root() as TotalRiskFundNode)?.CurrentDate ?? DateTime.Today; }
	//	}
	//	#endregion
	//	#region contents
	//	internal IEnumerable<NodeMap> CurrentMap { get; private set; }

	//	CommonNode _currentNode = RootCollection.Instance.LastOrDefault() ?? new TotalRiskFundNode() { CurrentDate = DateTime.Today };
	//	internal CommonNode CurrentNode {
	//		get {
	//			//if (_currentNode == null) _currentNode = new TotalRiskFundNode();
	//			return _currentNode;
	//		}
	//		private set {
	//			if (_currentNode == value) return;
	//			_currentNode = value;
	//			setCurrentMap();
	//			OnPropertyChanged(nameof(CurrentDate));
	//			OnPropertyChanged();
	//		}
	//	}
	//	public IEnumerable<DateTime> TimeAxis { get; private set; }

	//	internal string segmentSep(NodeMap map) {
	//		switch (Divide) {
	//		case _DividePattern.Tag:
	//			return map.Tag.TagName;
	//		default:
	//			return map.Node.Name;
	//		}
	//	}
	//	internal long mp(IEnumerable<IGrouping<DateTime,NodeMap>> src) {
	//		if (!src.Any()) return 0;
	//		return src.Last().Sum(a => a.Node.Amount);
	//	}
	//	public Dictionary<string,IEnumerable<long>> SegmentElement {
	//		get {
	//			var t = CurrentMap
	//				.ToLookup(a => segmentSep(a))
	//				.ToDictionary(a => a.Key, b => marge(b.GroupBy(c => c.Time), mp));
	//			return t;
	//		}
	//	}

	//	long selInv(long pre, IEnumerable<IGrouping<DateTime,NodeMap>> src) {
	//		long p;
	//		if (InvestmentUnit == _CalculationUnit.Total) p = pre;
	//		else p = 0;

	//		if (!src.Any()) return p;
	//		var nd = src.SelectMany(a => a);
	//		return p + nd.Sum(a => a.Node.InvestmentValue);// - nd.Sum(a => a.Node.InvestmentReturnValue);
	//	}
	//	public IEnumerable<long> InvestmentElement {
	//		get {
	//			return CurrentMap
	//				.ToLookup(a => segmentSep(a))
	//				.Select(b => marge(b.GroupBy(c => c.Time), selInv))
	//				.SelectMany(a => a);
	//		}
	//	}
	//	_Breakdown _details;
	//	public _Breakdown Details {
	//		get {
	//			if (_details == null) _details = new _Breakdown(this);
	//			return _details;
	//		}
	//	}
	//	#endregion

	//	#region method
	//	public void SetCurrent(DateTime date) {
	//		CurrentNode = RootCollection.GetNode(CurrentNode.Path, date);
	//	}
	//	#endregion

	//	void setCurrentMap() {
	//		var d = RootCollection.GetNodeLine(CurrentNode.Path)
	//			.ToDictionary(
	//				k => k.Key,//(k.Root() as TotalRiskFundNode).CurrentDate,
	//				v => mapping(v.Value, TargetLevel));
	//		var nodes = from tx in d
	//					from sx in tx.Value
	//					select new NodeMap() {
	//						Time = tx.Key,
	//						Node = sx.Value,
	//					};
	//		CurrentMap = nodes.ToArray();
	//		OnPropertyChanged(nameof(SegmentElement));
	//		OnPropertyChanged(nameof(InvestmentElement));
	//	}

	//	private void setTimeAxis() {
	//		IEnumerable<DateTime> rlt;
	//		var nds = RootCollection
	//			.GetNodeLine(CurrentNode.Path)
	//			.Keys;//.Select(a => (a as TotalRiskFundNode).CurrentDate);
	//		if (!nds.Any()) rlt = nds;
	//		DateTime ls = nds.Last();
	//		DateTime fs = nds.First();
	//		switch (TimePeriod) {
	//		case Period.Weekly:
	//			rlt = weeklyAxis(fs, ls);
	//			break;
	//		case Period.Monthly:
	//			rlt= monthlyAxis(fs, ls);
	//			break;
	//		case Period.Quarterly:
	//			rlt= quarterlyAxis(fs, ls);
	//			break;
	//		case Period.Yearly:
	//			rlt = yearlyAxis(fs, ls);
	//			break;
	//		default:
	//			rlt = Enumerable.Empty<DateTime>();
	//			break;
	//		}
	//		TimeAxis = rlt.ToArray();
	//		OnPropertyChanged(nameof(TimeAxis));
	//	}
		
	//	IEnumerable<long> marge(IEnumerable<IGrouping<DateTime,NodeMap>> src,Func<IEnumerable<IGrouping<DateTime,NodeMap>>,long> cur) {
	//		return marge(src, (p, s) => cur(s));
	//	}
	//	IEnumerable<long> marge(IEnumerable<IGrouping<DateTime, NodeMap>> src, Func<long,IEnumerable<IGrouping<DateTime, NodeMap>>, long> cur) {
	//		var s = src;
	//		long pre = 0;
	//		foreach (var ax in TimeAxis) {
	//			var tpl = split(s, ax);
	//			pre = cur(pre, tpl.Item1);
	//			yield return pre;
	//			s = tpl.Item2;
	//		}
	//	}

	//}
}
