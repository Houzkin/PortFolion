using LiveCharts;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin.Tree;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Livet;
using Houzkin.Architecture;
using PortFolion.IO;

namespace PortFolion.ViewModels {
	public enum Period {
		Yearly,
		Quarterly,
		Monthly,
		Weekly,
	}
	public enum DividePattern {
		Tag,
		Location,
	}
	public enum TransitionStatus {
		HideCashFlow,
		SingleCashFlow,
		StackCashFlow,
		ProfitLossOnly,
	}
	public enum VolatilityType {
		Normal,
		Log,
	}
	public class TempValue {
		public string Title { get; set; }
		public double Amount { get; set; }
		public double Rate { get; set; }
		public double Invest { get; set; }
		public NodeType Type { get; set; }
	}
	public static class Extension {
		/// <summary>
		/// 指定した項目種別で括る。
		/// </summary>
		public static IEnumerable<TempValue> MargeNodes(this CommonNode current, int tgtLv,DividePattern div) {
			var cd = current.NodeIndex().CurrentDepth;
			var ch = current.Height();
			var tg = Math.Min(ch, tgtLv);
			return current.Levelorder().Where(a => a.NodeIndex().CurrentDepth == cd + tg).MargeNodes(div);
		}
		private static IEnumerable<TempValue> MargeNodes(this IEnumerable<CommonNode> collection, DividePattern div) {
			Func<CommonNode, string> DivFunc;
			switch (div) {
			case DividePattern.Location:
				DivFunc = c => c.Name;
				break;
			default:
				DivFunc = c => c.Tag.TagName;
				break;
			}
			var ttl = (double)(collection.Sum(a => (a.Amount)));
			if (ttl == 0) return Enumerable.Empty<TempValue>();
			return collection
				.ToLookup(DivFunc)
				.Select(a => {
					var tv = new TempValue();
					tv.Title = a.Key;
					tv.Amount = a.Sum(b => b.Amount);
					tv.Rate = tv.Amount / ttl * 100;
					tv.Invest = a.Sum(b => b.InvestmentValue);
					tv.Type = a.First().GetNodeType();
					return tv;
				});
		}
	}
	public class GraphDataManager : DynamicViewModel {

		public GraphDataManager() : base(new GraphMediator()) {
			Model.Initialize(this);
		}
		public void Refresh() => Model.Refresh();
		GraphMediator Model => this.MaybeModelAs<GraphMediator>().Value;

		public BrakeDownList BrakeDown { get; } = new BrakeDownList();
		public TransitionList Transition { get; } = new TransitionList();
		public IndexList Index { get; } = new IndexList();
		public VolatilityList Volatility { get; } = new VolatilityList();

		
		
		private class GraphMediator : NotificationObject {
			GraphDataManager gdm;
			public GraphMediator() {
			}
			public void Initialize(GraphDataManager vm) {
				gdm = vm;
				Refresh();
			}
			#region properties
			public DateTime? CurrentDate {
				get { return (CurrentNode?.Root() as TotalRiskFundNode)?.CurrentDate; }
				set {
					if (CurrentDate == value) return;
					RaisePropertyChanged();
					// set currentNode
				}
			}
			CommonNode _commonNode;
			public CommonNode CurrentNode {
				get { return _commonNode; }
				private set {
					if (_commonNode == value) return;
					_commonNode = value;
					RaisePropertyChanged();
					RaisePropertyChanged(() => TargetLevel);
					RefreshBrakeDownList();
					//RefreshHistoryList();
				}
			}
			Period _timePeriod;
			public Period TimePeriod {
				get { return _timePeriod; }
				set {
					if (_timePeriod == value) return;
					_timePeriod = value;
					RaisePropertyChanged();
					RefreshHistoryList();
				}
			}
			int _targetLevel = 1;
			public int TargetLevel {
				get {
					if (CurrentNode == null) return _targetLevel;
					var hg = CurrentNode.Height();
					return Math.Min(_targetLevel, hg);
				}
				set {
					if (_targetLevel == value) return;
					_targetLevel = Math.Max(0, value);
					RaisePropertyChanged();
					RefreshBrakeDownList();
					RefreshHistoryList();
				}
			}
			int? _displayItemsCount;
			public int? DisplayItemsCount {
				get { return _displayItemsCount; }
				set {
					if (_displayItemsCount == value) return;
					_displayItemsCount = value;
					RaisePropertyChanged();
					RefreshBrakeDownList();
					RefreshHistoryList();
				}
			}
			DividePattern _divide;
			public DividePattern Divide {
				get { return _divide; }
				set {
					if (_divide == value) return;
					_divide = value;
					RaisePropertyChanged();
					RefreshBrakeDownList();
					RefreshHistoryList();
				}
			}
			TransitionStatus _investmentUnit;
			public TransitionStatus TransitionStatus {
				get { return _investmentUnit; }
				set {
					if (_investmentUnit == value) return;//transition も比較して設定
					_investmentUnit = value;
					RaisePropertyChanged();
					InvestmentUnitChanged();
				}
			}
			VolatilityType _volatilityType;
			public VolatilityType VolatilityType {
				get { return _volatilityType; }
				set {
					if (_volatilityType == value) return;
					_volatilityType = value;
					RaisePropertyChanged();
					VolatilityTypeChanged();
				}
			}
			#endregion
			public void Refresh() {

			}
			string[] OrderList;
			void SetOrderList() {
				var hs = new HashSet<string>();

			}
			void RefreshBrakeDownList() {
				var tgnss = CurrentNode.MargeNodes(TargetLevel, Divide).ToArray();
				//var tgnss = tgns.Where(a => a.Type != NodeType.Cash)
				//	.OrderByDescending(a=>a.Rate)
				//	.Concat(tgns.Where(a => a.Type == NodeType.Cash));
				gdm.BrakeDown.Clear();
				foreach (var data in tgnss) {
					gdm.BrakeDown.Add(
						new PieSeries() {
							Title = data.Title,
							Values = new ChartValues<ObservableValue>() { new ObservableValue(data.Rate) },
							DataLabels = true,
						});
				}
			}
			void RefreshHistoryList() {
				var b = RootCollection.GetNodeLine(CurrentNode.Path);
			}
			void InvestmentUnitChanged() { }
			void VolatilityTypeChanged() { }
		}
	}
	public class BrakeDownList : SeriesCollection { }
	public class TransitionList : SeriesCollection { }
	public class IndexList : SeriesCollection { }
	public class VolatilityList : SeriesCollection { }

	
}
