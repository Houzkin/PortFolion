using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using System.Windows.Input;
using Houzkin.Architecture;
using PortFolion.Core;
using Livet.Messaging;
using Livet.Commands;

namespace PortFolion.ViewModels{
    public class TagSettingViewModel : ViewModel {
        public IEnumerable<TagItem> Tags { get; }
        ViewModelCommand _execute;
        public ViewModelCommand ExecuteCmd =>
            _execute = _execute ?? new ViewModelCommand(() => { });
        ViewModelCommand _cancel;
        public ViewModelCommand CancelCmd =>
            _cancel = _cancel ?? new ViewModelCommand(() => { });

        TagItem _edittingItem;
        public TagItem EdittingItem {
            get { return _edittingItem; }
            private set {
                _edittingItem = value;
                this.RaisePropertyChanged();
            }
        }

        public void EditOrder(TagItem ti) {
            this.EdittingItem = ti;
            this.Messenger.Raise(new InteractionMessage("EditOrder"));
        }
        public void QuitOrder() {
            this.EdittingItem = null;
            this.Messenger.Raise(new InteractionMessage("QuitOrder"));
        }
        void _executeFunc() {
            Tags.Where(a => a.PresentTagName != a.NewTagName).ForEach(a => { });
            
        }
    }

    public class TagItem : DynamicViewModel<TagInfo> {
        TagSettingViewModel _vm;
        public TagItem(TagSettingViewModel vm, TagInfo tag): base(tag) {
            _vm = vm;
            _edittingTagName = tag.TagName;
            _presentTagName = tag.TagName;
            NewTagName = tag.TagName;
        }
        private string isAllowName() {
            var els = _vm.Tags.Except(new TagItem[] { this });
            if(els.Any(a=>a.NewTagName == this.EdittingTagName)) {
                return "既に存在します";
            } else {
                return "";
            }
        }

        ViewModelCommand _editCmd;
        public ViewModelCommand EditCmd =>
            _editCmd = _editCmd ?? new ViewModelCommand(() => {
                _vm.EditOrder(this);
            });

        ViewModelCommand _executeCmd;
        public ViewModelCommand ExecuteCmd =>
            _executeCmd = _executeCmd ?? new ViewModelCommand(() => {
                this.NewTagName = this.EdittingTagName;
                _vm.QuitOrder();
            }, () => !this.HasErrors);

        ViewModelCommand _cancelCmd;
        public ViewModelCommand CancelCmd =>
            _cancelCmd = _cancelCmd ?? new ViewModelCommand(() => {
                _vm.QuitOrder();
                this.NewTagName = this.PresentTagName;
            });
        /// <summary>編集されたタグ名</summary>
        public string NewTagName { get; private set; }

        string _edittingTagName;
        /// <summary>編集用公開プロパティ</summary>
        public string EdittingTagName {
            get { return _edittingTagName; }
            set {
                SetProperty(ref _edittingTagName, value, _ => isAllowName());
            }
        }
        string _presentTagName;
        public string PresentTagName {
            get { return _presentTagName; }
        }
    }
    
}
