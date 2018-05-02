using Houzkin.Architecture;
using Houzkin.Tree;
using Livet.Commands;
using Livet.Messaging;
using PortFolion.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PortFolion.ViewModels {
    /// <summary>タグ編集用のVMの基底クラス</summary>
    public abstract class NodeTagEditerVMBase<T> : DynamicViewModel<T> {
        public NodeTagEditerVMBase(T model) : base(model) {
            this.TagCollection = ReadOnlyBindableCollection.Create(
                TagInfo.GetList(),
                ti => {
                    var mi = new MenuItemVm(() => this.Tag = ti.TagName);
                    mi.Header = ti.TagName;
                    return mi;
                });
        }
        /// <summary>メッセンジャー</summary>
        public abstract InteractionMessenger Messenger { get; }
        /// <summary>候補をドロップダウンするためのタグコレクション</summary>
        public IEnumerable<MenuItemVm> TagCollection { get; }
        /// <summary>編集前のタグ</summary>
        public string PresentTag { get; protected set; }
        string _tag;
        public string Tag {
            get { return _tag; }
            set {
                SetProperty(ref _tag, value);
                this.ExecuteCmd.RaiseCanExecuteChanged();
            }
        }
        TagEditParam _editOption = TagEditParam.Position;
        /// <summary>編集オプション</summary>
        public TagEditParam TagEditOption {
            get { return _editOption; }
            set { SetProperty(ref _editOption, value); }
        }
        /// <summary>完了コマンド</summary>
        public abstract ViewModelCommand ExecuteCmd { get; }
        /// <summary>キャンセルコマンド</summary>
        public abstract ViewModelCommand CancelCmd { get; }
    }

    /// <summary>アカウント編集画面から使用するVM</summary>
    public class FromAccountNodeTagEditerVM : NodeTagEditerVMBase<CashEditVM> {
        AccountEditVM acc;
        public FromAccountNodeTagEditerVM(AccountEditVM ae,CashEditVM ce) : base(ce) {
            this.acc = ae;
            this.PresentTag = ce.Tag.TagName;
            this.Tag = ce.Tag.TagName;
        }
        public override InteractionMessenger Messenger
            => acc.Messenger;
        public string Name => Model.Name;
        void _executeFunc() {
            var tg = TagInfo.GetWithAdd(this.Tag.Trim());
            this.Model.Tag = tg;
            this.Model.TagEditOption = this.TagEditOption;
            this.acc.NodeTagEditer = null;
        }
        bool _canexecuteFunc() {
            var tg = this.Tag?.Trim();
            if (tg == PresentTag || string.IsNullOrEmpty(tg) || string.IsNullOrWhiteSpace(tg))
                return false;
            return true;
        }

        ViewModelCommand execute;
        public override ViewModelCommand ExecuteCmd
            => execute = execute ?? new ViewModelCommand(_executeFunc,_canexecuteFunc);
        ViewModelCommand cancel;
        public override ViewModelCommand CancelCmd
            => cancel = cancel ?? new ViewModelCommand(
                () => acc.NodeTagEditer = null); 
    }
    /// <summary>ロケーションツリーからの編集時に使用するVM</summary>
    public class NodeTagEditerVM : NodeTagEditerVMBase<CommonNode> {
        public NodeTagEditerVM(CommonNode model) : base(model) {
            this.PresentTag = model.Tag.TagName;
            this.Tag = model.Tag.TagName;
        }
        InteractionMessenger _messenger;
        public override InteractionMessenger Messenger
            => _messenger = _messenger ?? new InteractionMessenger();

        public string Title => "タグを変更";
        
        #region Commnad
        void _executeFunc() {
            if (!_canexecute())
                return;
            var tg = TagInfo.GetWithAdd(this.Tag.Trim());
            //edittinglistを編集
            var ds = TagInfo.Apply(this.Model, tg, this.TagEditOption);
            foreach (var d in ds)
                EdittingList.Add(d);
            Messenger.Raise(new InteractionMessage("EditEndNodeTag"));
        }
        bool _canexecute() {
            var tg = this.Tag?.Trim();
            if (tg == PresentTag || string.IsNullOrEmpty(tg) || string.IsNullOrWhiteSpace(tg))
                return false;
            return true;
        }
        ViewModelCommand execute;
        public override ViewModelCommand ExecuteCmd
            => execute = execute ?? new ViewModelCommand(_executeFunc, _canexecute);
        ViewModelCommand cancel;
        public override ViewModelCommand CancelCmd
            => cancel = cancel ?? new ViewModelCommand(
                () => Messenger.Raise(new InteractionMessage("EditEndNodeTag")));
        #endregion

        HashSet<DateTime> _edit;
        /// <summary>変更しているリスト</summary>
        public HashSet<DateTime> EdittingList => _edit = _edit ?? new HashSet<DateTime>();
    }
}
