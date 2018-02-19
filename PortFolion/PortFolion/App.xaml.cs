using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using Livet;
using System.Threading;
using System.Windows.Threading;

namespace PortFolion {
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application {

		/// <summary>多重起動を防止する為のミューテックス。</summary>
		//private static Mutex _mutex;

		private void Application_Startup(object sender, StartupEventArgs e) {
			DispatcherHelper.UIDispatcher = Dispatcher;

			var mainWindow = new PortFolion.Views.ModernMainWindow();
            mainWindow.DataContext = new PortFolion.ViewModels.MainWindowViewModel();
			mainWindow.Show();
		}
		private void Application_Exit(object sender, ExitEventArgs e) {
			//終了処理
			PortFolion.IO.CacheManager.Clear();
		}
		public static void DoEvent() {
			DispatcherFrame frame = new DispatcherFrame();
			var callback = new DispatcherOperationCallback(obj => {
				((DispatcherFrame)obj).Continue = false;
				return null;
			});
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
			Dispatcher.PushFrame(frame);
		}

		//集約エラーハンドラ
		//private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		//{
		//    //TODO:ロギング処理など
		//    MessageBox.Show(
		//        "不明なエラーが発生しました。アプリケーションを終了します。",
		//        "エラー",
		//        MessageBoxButton.OK,
		//        MessageBoxImage.Error);
		//
		//    Environment.Exit(1);
		//}
	}
}
