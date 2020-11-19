using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib;
using System.Windows.Forms;
using System.Dynamic;
using System.Reflection;

namespace PS_AppearanceInspecion
{
    static class Program
    {
        /// <summary>
        /// Main()のログ出力
        /// </summary>
        static Log m_log = null;

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // アプリケーション名
            string appName = Util.GetAppTitle();
            // アプリケーションのバージョンを取得
            string appVer = Util.GetAppVersion();
            // アプリケーションの製品名
            string appProduct = Util.GetAppProduct();

            // 二重起動チェック
            Mutex chkmutex = new Mutex(false, appProduct + "Mutex");
            if (!chkmutex.WaitOne(0, false))
            {
                CommonUI.MsgBox.Error("二重起動エラー。終了します。");
                return;
            }

            // カレントディレクトリをexeの階層にする
            Util.ChangeCurrentDir();

            // 捕捉出来ない例外が発生したときのイベント
            AppDomain.CurrentDomain.UnhandledException += (_sender, _earg) => UnhandledExceptionHandler(_earg.ExceptionObject);

            // アプリ設定ファイルのリード
            string errStr;
            var cfg = Config.ConfigDataManager.LoadAppConfig(appProduct + ".xml", out errStr);
            if (cfg == null)
            {
                CommonUI.MsgBox.Error("設定ファイルリードエラー\r\n" + errStr);
                return;
            }

            // ログ機能開始
            Log.LogFilePrefix = "PSAI_";
            Log.LogDir = cfg.LogDir;
            Log.LogFilter = cfg.LogFilter;
            Log.LogAutoEraseDays = cfg.LogAutoEraseDays;
            if (!Log.Open())
            {
                CommonUI.MsgBox.Error("ログ出力開始エラー\r\n{0}", Log.ErrorMsg);
                return;
            }
            // ログ出力
            m_log = new Log("Main");
            m_log.System("{0}({1}) 開始", appName, appVer);

            CommonUI.FormBase.CreateBackGroundForm();

            var ctr = new Controllers.ControllerMain(cfg);
            ctr.MainProc();

            CommonUI.FormBase.CloseBackGroundForm();

            m_log.System("{0}({1}) 終了", appName, appVer);
            // ログ機能停止
            Log.Close();
        }

        /// <summary>
        /// catch出来ない例外が発生したときのハンドラ
        /// </summary>
        /// <param name="eObj">Exceptionインスタンス</param>
        static void UnhandledExceptionHandler(object eObj)
        {
            try
            {
                if (m_log != null)
                {
                    m_log.Error("UnhandledException 終了します。 : {0}", eObj);
                }
            }
            finally
            {
                Environment.Exit(1);
            }
        }
    }

}
