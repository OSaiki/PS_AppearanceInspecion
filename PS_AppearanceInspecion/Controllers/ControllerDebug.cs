using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Dynamic;
using System.Threading;
using CommonLib;
using CommonUI;

namespace PS_AppearanceInspecion.Controllers
{
    /// <summary>
    /// デバッグモード処理クラス
    /// </summary>
    class ControllerDebug : Controller.ControllerBase
    {
        protected Config.ConfigDataManager m_cfg;
        protected string m_recipeNo = null;
        protected Operator m_user = null;
        protected Device.DeviceManager m_devMgr;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="cfg">設定クラス</param>
        /// <param name="dev">デバイス管理クラス</param>
        public ControllerDebug(Config.ConfigDataManager cfg, Device.DeviceManager dev)
        {
            m_cfg = cfg;
            m_devMgr = dev;
        }

        /// <summary>
        /// デバッグモード主処理
        /// </summary>
        /// <returns>正常終了/異常終了</returns>
        public bool MainProc()
        {
            log.System("デバッグモード 開始");

            // デバッグモード画面表示
            UI.FormDebugMode fmdbg = new UI.FormDebugMode(m_devMgr.DeviceTable);
            var fmtask = UITask(fmdbg);


            // デバッグモード画面終了待ち
            fmtask.Wait();

            bool bret = fmtask.Result == FormResult.OK;

            log.System("デバッグモード 終了 : 結果={0}", bret);

            return bret;
        }

        /// <summary>
        /// CIMコマンド実行ハンドラ
        /// </summary>
        /// <param name="cimprm">パラメータ(FTP接続情報)</param>
        private void OnActionCIM(ActionParam_CIMCommand cimprm)
        {
            // 現在のCIM設定値取得
            ExpandoObject eo = m_cfg.GetDeviceConfig(Device.DevType.CIM);
            IDictionary<string, object> dic = eo;

            bool bret = false;
            switch (cimprm.Cmd)
            {
                case ActionParam_CIMCommand.Command.GetParam:
                    // 設定値取得
                    cimprm.ServerPath = dic["ServerPath"].ToString();
                    cimprm.User = dic["User"].ToString();
                    cimprm.Password = dic["Password"].ToString();
                    bret = true;
                    break;

                case ActionParam_CIMCommand.Command.Connect:
                    // 接続先変更
                    bret = m_devMgr.CIM.ChangeConnectionFTPServer(cimprm.ServerPath, cimprm.User, cimprm.Password);
                    break;

                case ActionParam_CIMCommand.Command.Update:
                    // 設定値のサーバアドレス、ユーザ名、パスワード、を書き換える
                    dic["ServerPath"] = cimprm.ServerPath;
                    dic["User"] = cimprm.User;
                    dic["Password"] = cimprm.Password;
                    // 設定データの更新
                    bret = m_devMgr.CIM.SetParam(eo);
                    if (bret)
                    {
                        // 設定ファイルの部分更新
                        bret = m_cfg.UpdateConfigFile(eo);
                    }
                    break;

            }
            cimprm.result = bret;
        }

        /// <summary>
        /// プリンターコマンド実行ハンドラ
        /// </summary>
        /// <param name="cimprm">パラメータ(FTP接続情報)</param>
        private void OnActionPrinter(ActionParam_PrintrtCommand prtprm)
        {
            // 現在のCIM設定値取得
            ExpandoObject eo = m_cfg.GetDeviceConfig(Device.DevType.Printer);
            IDictionary<string, object> dic = eo;

            bool bret = false;
            switch (prtprm.Cmd)
            {
                // UIからの依頼。印刷する
                case ActionParam_PrintrtCommand.Command.PrintOut:
                    bret = m_devMgr.Printer.PrintOutID(prtprm.Kind,
                        prtprm.PanelID,
                        prtprm.ModuleID,
                        prtprm.TemplateFileNameQR,
                        prtprm.PrintNumQR,
                        prtprm.TemplateFileNameText,
                        prtprm.PrintNumText);
                    break;

            }
            prtprm.result = bret;
        }


        /// <summary>
        /// 指定の機器に対して初期化と接続確認を実行するタスク
        /// </summary>
        /// <param name="dtype">機器種別</param>
        /// <returns>接続確認タスク</returns>
        protected Task<bool> DeviceConnectTask(Enum dtype)
        {
            Task<bool> connect_task = Task.Run(() => {
                // 初期化
                bool bret = m_devMgr.Initialize(dtype);
                if (bret)
                {
                    // 接続
                    bret = m_devMgr.Connect(dtype);
                }
                return bret;
            });

            return connect_task;
        }

        /// <summary>
        /// CIMコマンド実行ハンドラ
        /// </summary>
        /// <param name="param">パラメータ(機器情報)</param>
        private void OnActionDev(ActionParam_DeviceConnect param)
        {
            // 機器の指定があるか？
            if (!param.IsAllConnect)
            {
                Enum dev = param.Target;
                // 指定機器の接続確認のタスク
                var task = DeviceConnectTask(dev);
                try
                {
                    // 接続確認の終了を待つ
                    task.Wait(m_cts.Token);
                }
                catch (Exception e)
                {
                    log.Warning("{0}接続確認でエラー:{1}", dev, e);
                }
            }
            else
            {
                // 全機器の接続確認のタスクをリスト化
                var tasks = m_devMgr.DevTypes.Select(dvtp => DeviceConnectTask(dvtp));

                try
                {
                    // 接続確認の終了を待つ
                    Task.WaitAll(tasks.ToArray(), m_cts.Token);
                }
                catch (Exception e)
                {
                    log.Warning("全機器接続確認でエラー:{0}", e);
                }

            }
        }

        /// <summary>
        /// UIからの要求を実行
        /// </summary>
        /// <param name="aParam">アクションパラメータ</param>
        override protected void OnControllerAction(ActionParam_Base aParam)
        {
            if (aParam is ActionParam_CIMCommand)
            {
                // デバッグモード CIMコマンド
                OnActionCIM(aParam as ActionParam_CIMCommand);
            }
            else if (aParam is ActionParam_PrintrtCommand)
            {
                // デバッグモード プリンターコマンド
                OnActionPrinter(aParam as ActionParam_PrintrtCommand);
            }
            else if (aParam is ActionParam_DeviceConnect)
            {
                // デバッグモード 機器接続コマンド
                OnActionDev(aParam as ActionParam_DeviceConnect);
            }
            else
            {
                base.OnControllerAction(aParam);
            }

        }
    }
}
