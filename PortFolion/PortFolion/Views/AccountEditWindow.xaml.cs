﻿using Livet.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PortFolion.Views {
	/* 
	 * ViewModelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedWeakEventListenerや
     * CollectionChangedWeakEventListenerを使うと便利です。独自イベントの場合はLivetWeakEventListenerが使用できます。
     * クローズ時などに、LivetCompositeDisposableに格納した各種イベントリスナをDisposeする事でイベントハンドラの開放が容易に行えます。
     *
     * WeakEventListenerなので明示的に開放せずともメモリリークは起こしませんが、できる限り明示的に開放するようにしましょう。
     */

	/// <summary>
	/// AccountEditWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class AccountEditWindow : MahApps.Metro.Controls.MetroWindow {
		public AccountEditWindow() {
			InitializeComponent();
		}
		//ViewModelCommand oennf;
		//public ICommand OpenEditNodeNameFlyout 
		//	=> oennf = oennf ?? new ViewModelCommand(() => this.NameEditFlyout.IsOpen = true);
		public void OpenNameEditFlyout() => this.NameEditFlyout.IsOpen = true;
		//ViewModelCommand cennf;
		//public ICommand CloseEditNodeNameFlyout
		//	=> cennf = cennf ?? new ViewModelCommand(() => this.NameEditFlyout.IsOpen = false);
		public void CloseNameEditFlyout() => this.NameEditFlyout.IsOpen = false;
	}
}