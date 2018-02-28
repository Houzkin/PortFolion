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
        public TagSettingViewModel() {
            Tags = ReadOnlyBindableCollection
                .Create(TagInfo.GetList(), m => new TagItem(this, m));
            this.CompositeDisposable.Add(Tags as IDisposable);
        }

        public IEnumerable<TagItem> Tags { get; }
        ViewModelCommand _execute;
        /// <summary>一連の編集を完了する</summary>
        public ViewModelCommand ExecuteCmd =>
            _execute = _execute ?? new ViewModelCommand(_executeFunc, _canExecuteFunc);
        ViewModelCommand _cancel;
        /// <summary>一連の編集をキャンセルする</summary>
        public ViewModelCommand CancelCmd =>
            _cancel = _cancel ?? new ViewModelCommand(_closeWindow);

        void _executeFunc() {
			var dic = new Dictionary<TagInfo, string>();
			foreach (var a in Tags.Select(a => a.GetChangeOrder()).Where(b => b != null))
				dic[a.Item1] = a.Item2;
			TagInfo.EditTagName(dic);
            _closeWindow();
        }
        bool _canExecuteFunc() =>
            this.Tags.Any(a => a.HasChanged);

        void _closeWindow() =>
            this.Messenger.Raise(new InteractionMessage("CloseWindow"));
        
        TagItem _edittingItem;
        public TagItem EdittingItem {
            get { return _edittingItem; }
            private set {
                _edittingItem = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>個別編集の開始</summary>
        /// <param name="ti">編集する対象</param>
        public void EditOrder(TagItem ti) {
			if (this.EdittingItem == ti)
				return;
			if (this.EdittingItem != null)
				this.Messenger.Raise(new InteractionMessage("QuitOrder"));
            this.EdittingItem = ti;
            this.Messenger.Raise(new InteractionMessage("EditOrder"));
        }
        /// <summary>個別編集の終了</summary>
        public void QuitOrder() {
            this.EdittingItem = null;
            this.Messenger.Raise(new InteractionMessage("QuitOrder"));
            this.ExecuteCmd.RaiseCanExecuteChanged();
        }
    }
	/// <summary>タグデータのViewModel</summary>
    public class TagItem : DynamicViewModel<TagInfo> {
        TagSettingViewModel _vm;
        public TagItem(TagSettingViewModel vm, TagInfo tag): base(tag) {
            _vm = vm;
            _edittingTagName = tag.TagName;
            _previousTagName = tag.TagName;
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
        /// <summary>変更がある場合はtrueを返す</summary>
        public bool HasChanged => this.PreviousTagName != this.NewTagName;
        /// <summary>変更があった場合、対象と変更先タグ名のペアを返す</summary>
        public Tuple<TagInfo,string> GetChangeOrder() {
            if (!HasChanged) return null;
			return new Tuple<TagInfo, string>(this.Model, this.NewTagName);
        }

        ViewModelCommand _editCmd;
        /// <summary>編集を開始するためのコマンド</summary>
        public ViewModelCommand EditCmd =>
            _editCmd = _editCmd ?? new ViewModelCommand(() => {
                _vm.EditOrder(this);
            });

        ViewModelCommand _executeCmd;
        /// <summary>編集を完了するためのコマンド</summary>
        public ViewModelCommand ExecuteCmd =>
            _executeCmd = _executeCmd ?? new ViewModelCommand(() => {
                this.NewTagName = this.EdittingTagName;
                _vm.QuitOrder();
            }, () => !this.HasErrors);

        ViewModelCommand _cancelCmd;
        /// <summary>編集をキャンセルするためのコマンド</summary>
        public ViewModelCommand CancelCmd =>
            _cancelCmd = _cancelCmd ?? new ViewModelCommand(() => {
                _vm.QuitOrder();
                this.EdittingTagName = this.NewTagName;
            });
        /// <summary>編集されたタグ名</summary>
        public string NewTagName { get; private set; }

        string _edittingTagName;
        /// <summary>編集用公開プロパティ</summary>
        public string EdittingTagName {
            get { return _edittingTagName; }
            set {
                SetProperty(ref _edittingTagName, value, _ => isAllowName());
                this.ExecuteCmd.RaiseCanExecuteChanged();
            }
        }
        string _previousTagName;
        /// <summary>非編集用、変更前のタグ名</summary>
        public string PreviousTagName {
            get { return _previousTagName; }
        }
    }
    
}
