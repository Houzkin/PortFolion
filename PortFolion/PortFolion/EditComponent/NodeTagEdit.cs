using Houzkin.Architecture;
using Houzkin.Tree;
using Livet.Commands;
using Livet.Messaging;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PortFolion.ViewModels {
    /// <summary>タグ編集オプション</summary>
    public enum EditHistoryParam {
        CurrentOnly,
        Position,
        AllHistory,
    }
    public class NodeTagEditerVM : DynamicViewModel<CommonNode> {
        public NodeTagEditerVM(CommonNode model) : base(model) {
            this.PresentTag = model.Tag.TagName;
            this._tag = model.Tag.TagName;
        }
        InteractionMessenger _messenger;
        public InteractionMessenger Messenger
            => _messenger = _messenger ?? new InteractionMessenger();

        public string Title => "タグの変更";
        /// <summary>変更前のタグ</summary>
        public string PresentTag { get; }
        string _tag;
        /// <summary>バインド用のタグ名</summary>
        public string Tag {
            get { return _tag; }
            set {
                SetProperty(ref _tag, value);
                this.ExecuteCmd.RaiseCanExecuteChanged();
            }
        }
        EditHistoryParam _editOption = EditHistoryParam.Position;
        /// <summary>編集オプション</summary>
        public EditHistoryParam EditOption {
            get { return _editOption; }
            set { SetProperty(ref _editOption, value); }
        }
        public IEnumerable<string> TagCollection
            => ReadOnlyBindableCollection.Create(TagInfo.GetList(), m => m.TagName);

        #region Commnad
        void _execute() {
            var tg = _tag?.Trim() ?? "";
            //edittinglistを編集
            switch (EditOption) {
            case EditHistoryParam.CurrentOnly:
                this.Model.Tag.EditTagName(tg);
                EdittingList.Add((this.Model.Root() as TotalRiskFundNode).CurrentDate);
                break;
            case EditHistoryParam.AllHistory:
                var dic = RootCollection.GetNodeLine(this.Model.Path);
                foreach(var d in dic) {
                    d.Value.Tag.EditTagName(tg);
                    EdittingList.Add(d.Key);
                }
                break;
            case EditHistoryParam.Position:
                var dd = RootCollection.GetNodeLine(this.Model.Path, (this.Model.Root() as TotalRiskFundNode).CurrentDate);
                foreach (var d in dd) {
                    d.Value.Tag.EditTagName(tg);
                    EdittingList.Add(d.Key);
                }
                break;
            }
            Messenger.Raise(new InteractionMessage("EditEndNodeTag"));
        }
        bool _canexecute() {
            var tg = _tag?.Trim();
            if (tg == PresentTag || string.IsNullOrEmpty(tg) || string.IsNullOrWhiteSpace(tg))
                return false;
            return true;
        }
        ViewModelCommand execute;
        public ViewModelCommand ExecuteCmd
            => execute = execute ?? new ViewModelCommand(_execute, _canexecute);
        ViewModelCommand cancel;
        public ViewModelCommand CancelCmd
            => cancel = cancel ?? new ViewModelCommand(
                () => Messenger.Raise(new InteractionMessage("EditEndNodeTag")));
        #endregion

        HashSet<DateTime> _edit;
        public HashSet<DateTime> EdittingList => _edit = _edit ?? new HashSet<DateTime>();
    }
}
