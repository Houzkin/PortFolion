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

        #region Commnad
        void _execute() {
            //edittinglistを編集
            switch (EditOption) {
            case EditHistoryParam.CurrentOnly:
                this.Model.Tag.EditTagName(_tag.Trim());
                break;
            case EditHistoryParam.AllHistory:
                var dic = RootCollection.GetNodeLine(this.Model.Path);
                break;
            case EditHistoryParam.Position:
                break;
            }
        }
        bool _canexecute() {
            var tg = _tag.Trim();
            if (tg == PresentTag || string.IsNullOrEmpty(tg) || string.IsNullOrWhiteSpace(tg))
                return false;
            return true;
        }
        ViewModelCommand execute;
        public ViewModelCommand ExecuteCmd
            => execute = execute ?? new ViewModelCommand(_execute, _canexecute);
        ViewModelCommand cancel;
        public ViewModelCommand CancelCmd
            => cancel = cancel ?? new ViewModelCommand(() => { });
        #endregion

        HashSet<DateTime> _edit;
        public HashSet<DateTime> EdittingList => _edit = _edit ?? new HashSet<DateTime>();
    }
}
