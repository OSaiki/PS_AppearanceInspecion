using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using CommonLib;

namespace PS_AppearanceInspecion.Controllers
{
    /// <summary>
    /// メイン処理クラス
    /// </summary>
    class ControllerMain : Controller.ControllerBase
    {
        protected Config.ConfigDataManager m_cfg;
        protected string m_recipeNo = null;
        protected Operator m_user = null;
        protected Device.DeviceManager m_devMgr;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="cfg">設定クラス</param>
        public ControllerMain(Config.ConfigDataManager cfg) 
        {
            m_cfg = cfg;
        }

        /// <summary>
        /// 主処理
        /// </summary>
        public void MainProc()
        {
            // 機器設定ファイル
            if (!m_cfg.LoadDeviceConfigFiles())
            {
                CommonUI.MsgBox.Error("機器設定ファイルリードエラー");
                return;
            }

            // レシピファイルの一覧を取得
            if (!m_cfg.GetRecipeFiles())
            {
                CommonUI.MsgBox.Error("検査レシピファイル検索エラー");
                return;
            }
            if (m_cfg.RecipeFileList.Count == 0)
            {
                CommonUI.MsgBox.Error("検査レシピファイルが1件も存在しません");
                return;
            }
            log.Info("検査レシピ検出完了。 {0}件存在します : [{1}]", m_cfg.RecipeFileList.Count, string.Join(", ", m_cfg.RecipeFileList.Keys));

            // 作業者リストファイルから作業者リストを取得
            if (!m_cfg.ReadOperatorList())
            {
                CommonUI.MsgBox.Error("作業者リストファイルリードエラー");
                return;
            }

            // ユーザデータファイルの読み込み
            if (!m_cfg.LoadUserData())
            {
                // LoadUserData内でエラーポップアップを表示しているので何もしない
                return;
            }

            // 機器管理クラス
            m_devMgr = new Device.DeviceManager();
            // キー入力監視開始
            m_devMgr.StartKeyInput();
            m_devMgr.InputID += OnDeviceInputId;
            // 各機器のインスタンス作成
            if (!m_devMgr.CreateDevices())
            {
                CommonUI.MsgBox.Error("機器インスタンス生成エラー");
                return;
            }
            Enum err_dev;
            // 各機器のパラメータ設定
            if (!m_devMgr.SetParamAll(m_cfg, out err_dev))
            {
                CommonUI.MsgBox.Error($"機器設定値エラー({err_dev})");
                return;
            }
            // ライブラリ情報をログ出力
            Util.GetDllInfos().ForEach(info => log.Info("ライブラリ情報 {0}", info));

            var fmInput = new CommonUI.FormInputID(m_cfg.RecipeFileList.Keys, m_cfg.OperatorList);
            // ID入力画面表示
            var fmtask = UITask(fmInput);


            // ID入力画面終了待ち
            fmtask.Wait();

            if (m_user == null)
            {
                // ID入力画面で「終了」選択時
                return;
            }

            bool ctrRet;
            if (m_cfg.Mode != Config.ConfigBase.AppMode.Debug && m_user.ID != "9999999999")
            {
                // 検査モード
                var ctrlIns = new ControllerInspection(m_cfg, m_devMgr, m_recipeNo, m_user);
                ctrRet = ctrlIns.MainProc();

                string insErrorMsg = ctrlIns.GetErrorMsg();
                if (ctrRet)
                {
                    // エラー無し、正常にアプリを終了の場合
                    m_cfg.UnsendDataList = m_devMgr.CIM.GetUnsendDataList();
                    m_cfg.SaveUserData();

                    // 消灯
                    m_devMgr.PLC.SignalTower(Device.SigType.None);
                }
                else 
                {
                    Task cimTask = Task.Run(() =>
                    {
                        // CIMにエラー報告
                        UploadEquipmentError();

                        // CIMから未送信データリストを取得、ユーザーデータ更新
                        m_cfg.UnsendDataList = m_devMgr.CIM.GetUnsendDataList();
                        m_cfg.SaveUserData();
                    });

                    // エラーメッセージが設定済みかどうか
                    if (insErrorMsg != null)
                    {
                        // エラーメッセージボックス表示
                        Task msgboxTask = Task.Run(() => CommonUI.MsgBox.Error(insErrorMsg));

                        // 赤点灯 ＆ ブザー音(連続音)
                        m_devMgr.PLC.SignalTower(Device.SigType.Red | Device.SigType.BuzCont);

                        // エラーメッセージボックスが閉じられるのを待機する
                        msgboxTask.Wait();
                    }

                    // CIMエラー送信タスクの終了を待機する
                    cimTask.Wait();

                    // シグナルタワー赤（ブザー音無し）
                    m_devMgr.PLC.SignalTower(Device.SigType.Red);
                }
            }
            else
            {
                // デバッグモード
                var ctrlDbg = new ControllerDebug(m_cfg, m_devMgr);
                ctrRet = ctrlDbg.MainProc();
            }

            // 機器終了処理
            m_devMgr.Exit();
            // キー入力監視終了
            m_devMgr.InputID -= OnDeviceInputId;
            m_devMgr.StopKeyInput();
        }


        /// <summary>
        /// 設備異常報告
        /// </summary>
        /// <returns>成功/失敗</returns>
        bool UpdateError()
        {
            // 前回のエラー有無
            CIMManager.EquipmentStatusKind le = m_cfg.LastErrorKind;

            switch (le)
            {
                // 正常復帰 → 処理なし
                case CIMManager.EquipmentStatusKind.ReturnNormal:
                    break;

                // 異常停止 → 正常復帰
                case CIMManager.EquipmentStatusKind.AbnormalStop:
                // 警告 → 正常復帰
                case CIMManager.EquipmentStatusKind.Warning:
                    if (m_cfg.NowError == CIMManager.EquipmentStatusKind.ReturnNormal)
                    {
                        log.Info("設備異常報告更新");

                        // CIMにファイル送信
                        // （暫定）復帰時のコードは「0000」、メッセージは空文字列とする
                        if (!m_devMgr.CIM.SetEquipmentError("0000", CIMManager.EquipmentStatusKind.ReturnNormal, ""))
                        {
                            return false;
                        }

                        // 設備状態更新
                        m_cfg.LastErrorKind = CIMManager.EquipmentStatusKind.ReturnNormal;
                        if (!m_cfg.SaveUserData())
                        {
                            return false;
                        }
                    }
                    break;

                default:
                    // 未対応の設備状態
                    log.Error("設備状態未対応:{0}", le);
                    return false;
            }

            return true;
        }


        /// <summary>
        /// 設備異常報告送信処理<br/>設備異常報告を送信する。
        /// </summary>
        /// <returns>成功/失敗</returns>
        private bool UploadEquipmentError()
        {
            string errMessage = "";
            int errCode = 0;

            // TODO:エラーコード設定 ★
            errCode = 1234;//★
            errMessage = "Soft Error.";

            m_devMgr.CIM.SetEquipmentError(errCode.ToString("D4"), CIMManager.EquipmentStatusKind.AbnormalStop, errMessage);

            // 設備状態更新
            m_cfg.LastErrorKind = CIMManager.EquipmentStatusKind.AbnormalStop;
            if (!m_cfg.SaveUserData())
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// UIからのイベント
        /// </summary>
        /// <param name="aParam">パラメータ</param>
        override protected void OnControllerAction(CommonUI.ActionParam_Base aParam)
        {
            base.OnControllerAction(aParam);

            // ID入力完了
            if (aParam is CommonUI.ActionParam_IdInput)
            {
                CommonUI.ActionParam_IdInput prm = aParam as CommonUI.ActionParam_IdInput;
                if (prm.NewOperator)
                {
                    if (!m_cfg.AddToOperatorList(prm.User))
                    {
                        prm.result = false;
                        return;
                    }
                }
                m_recipeNo = prm.RecipeNo;
                m_user = prm.User;
            }
        }

        /// <summary>
        /// 文字列化
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "コントローラー(メイン)";
        }
    }

}
