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
    
    
    public class FromAccountEditerTagEditVM: NodeTagEditerVM {
        AccountEditVM acc;
        public FromAccountEditerTagEditVM(AccountEditVM ae,CashEditVM cevm)
            : base(cevm.Model) {
            this.acc = ae;
        }
        public override InteractionMessenger Messenger
            => this.acc.Messenger;
        protected override void ExecuteFunc() {
            if(acc.Model != Model.Parent) {
                //かきかけ
                //Model.Tag;
            } else {
                base.ExecuteFunc();
            }
        }
    }

    public class NodeTagEditerVM : DynamicViewModel<CommonNode> {
        public NodeTagEditerVM(CommonNode model) : base(model) {
            this.PresentTag = model.Tag.TagName;
            this._tag = model.Tag.TagName;

            this.TagCollection = ReadOnlyBindableCollection.Create(
                TagInfo.GetList(), 
                ti => {
                    var mi = new MenuItemVm(() => this.Tag = ti.TagName, () => this.Tag != ti.TagName);
                    mi.Header = ti.TagName;
                    return mi;
                });
        }
        InteractionMessenger _messenger;
        public virtual InteractionMessenger Messenger
            => _messenger = _messenger ?? new InteractionMessenger();

        public string Title => "タグを変更";
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
        TagEditParam _editOption = TagEditParam.Position;
        /// <summary>編集オプション</summary>
        public TagEditParam EditOption {
            get { return _editOption; }
            set { SetProperty(ref _editOption, value); }
        }
        public IEnumerable<MenuItemVm> TagCollection { get; }
            //=> ReadOnlyBindableCollection.Create(TagInfo.GetList(), m => m.TagName);

        #region Commnad
        protected virtual void ExecuteFunc() {
            if (!_canexecute())
                return;
            var tg = TagInfo.GetWithAdd(_tag.Trim());
            //edittinglistを編集
            var ds = TagInfo.Apply(this.Model, tg, this.EditOption);
            foreach (var d in ds)
                EdittingList.Add(d);
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
            => execute = execute ?? new ViewModelCommand(ExecuteFunc, _canexecute);
        ViewModelCommand cancel;
        public ViewModelCommand CancelCmd
            => cancel = cancel ?? new ViewModelCommand(
                () => Messenger.Raise(new InteractionMessage("EditEndNodeTag")));
        #endregion

        HashSet<DateTime> _edit;
        public HashSet<DateTime> EdittingList => _edit = _edit ?? new HashSet<DateTime>();
    }
}
