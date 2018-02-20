using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using System.Windows.Input;
using Houzkin.Architecture;
using PortFolion.Core;

namespace PortFolion.ViewModels{
    public class TagSettingViewModel : ViewModel {
        public IEnumerable<TagItem> Tags { get; }
        public ICommand ExecuteCmd { get; }
        public ICommand CancelCmd { get; }
        //public bool CanEditable(string str) {

        //}
    }

    public class TagItem : DynamicViewModel<TagInfo> {
        TagSettingViewModel _vm;
        public TagItem(TagSettingViewModel vm, TagInfo tag): base(tag) {
            _vm = vm;
        }
        public string CurrentTagName { get; }
        public string PresentTagName { get; }
    }
    
}
