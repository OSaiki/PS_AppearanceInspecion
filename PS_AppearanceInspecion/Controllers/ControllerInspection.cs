using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CommonLib;
using Inspection;

namespace PS_AppearanceInspecion.Controllers
{
    /// <summary>
    /// 検査処理クラス
    /// </summary>
    class ControllerInspection : Controller.ControllerBase
    {
        protected Config.ConfigDataManager m_cfg;
        protected string m_recipeNo = null;
        protected Operator m_user = null;
        protected Device.DeviceManager m_devMgr;
        string m_chassisID;
        string m_moduleID;
        string m_panelID;
        InspectionList m_insList;
        InspectionStep m_step;
        CommonUI.FormBase m_fmMain;
        FormMessages.UpdateStepInfo m_updStep;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="cfg">設定クラス</param>
        /// <param name="_recipeNo">レシピ番号</param>
        /// <param name="_user">作業者</param>
        public ControllerInspection(Config.ConfigDataManager cfg, Device.DeviceManager dev, string _recipeNo, Operator _user)
        {
            m_recipeNo = _recipeNo;
            m_user = _user;
            m_devMgr = dev;
            m_cfg = cfg;
        }

        /// <summary>
        /// 状態の設定
        /// </summary>
        /// <param name="newStep">次の状態</param>
        void SetStep(InspectionStep newStep)
        {
            m_step = newStep;
            m_updStep = new FormMessages.UpdateStepInfo(newStep);
            m_fmMain.PostMessage(FormMessages.UpdateStep(m_updStep));
        }

        ProcResult WaitFinish()
        {
            try
            {
                // 終了待ち
                m_updStep.WaitFinish(m_cts.Token);
            }
            catch (OperationCanceledException)
            {
                log.Info("強制的にキャンセルしました");
                return ProcResult.Cancel;
            }
            return ProcResult.Success;
        }

        /// <summary>
        /// 外観検査モード主処理
        /// </summary>
        /// <returns>正常終了/異常終了</returns>
        public bool MainProc()
        {
            log.System("検査モード 開始");
            string recipe_fpath = m_cfg.RecipeFileList[m_recipeNo];
            // レシピファイルリード
            m_insList = InspectionList.LoadRecipeFile(recipe_fpath);
            if (m_insList == null)
            {
                CommonUI.MsgBox.Error($"検査レシピ取得エラー\r\nレシピ'{m_recipeNo}'をリード中に異常がありました");
                return false;
            }
            // デバイス異常時のコールバック設定
            m_devMgr.OnErrorEnd += OnDeviceErrorEnd;

            m_devMgr.InputID += OnDeviceInputId;
            m_step = InspectionStep.None;

            // プリンタの使用有無設定更新
            m_devMgr.Printer.Use = !string.IsNullOrEmpty(m_insList.TemplateFileNameBarcode);

            // メイン画面生成
            m_fmMain = new UI.FormAppearanceInspection(m_recipeNo, m_user, m_insList);
            var fmtask = UITask(m_fmMain);

            // 検査処理タスク
            var insTask = Task.Factory.StartNew(MainInspectionTask, TaskCreationOptions.LongRunning);

            insTask.ContinueWith(t => {
                if (t.Result == ProcResult.Error)
                {
                    m_fmMain.Destroy();
                }
            });

            try
            {
                // メイン画面終了待ち
                fmtask.Wait(m_cts.Token);
            }
            catch (Exception)
            {
                m_cts.Cancel();
            }
            m_cts.Dispose();


            m_devMgr.InputID -= OnDeviceInputId;
            bool bret = fmtask.Result == CommonUI.FormResult.OK　|| fmtask.Result == CommonUI.FormResult.Cancel;
            log.System("検査モード 終了 : 結果={0}", bret);
            return bret;
        }


        /// <summary>
        /// エラー切断時ハンドラ
        /// </summary>
        /// <param name="type">機器種別</param>
        /// <param name="msg">エラーメッセージ</param>
        private void OnDeviceErrorEnd(Enum type, string msg)
        {
            SetErrorMsg($"エラーによる切断({type}) : {msg}");
            // メイン処理を止める
            m_cts.Cancel();
        }


        /// <summary>
        /// シグナルタワー更新
        /// </summary>
        /// <param name="tp">点灯パターン</param>
        /// <returns>成功/失敗</returns>
        bool SetSignalTower(Device.SigType tp)
        {
            if (!m_devMgr.PLC.SignalTower(tp))
            {
                SetErrorMsg("PLCシグナルタワー設定エラー");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 検査ループ
        /// </summary>
        /// <returns>処理結果</returns>
        ProcResult MainInspectionTask()
        {
            ProcResult ret;

            // Step0 「初期化中」
            ret = Proc_Intialize();
            if (ret != ProcResult.Success) return ret;

            // パネル検査ループ。パネル1枚処理毎に1周する
            while (true)
            {
                // Step1 「ID取得」
                ret = Proc_GetID();
                if (ret != ProcResult.Success) break;

                // Step2 「パネルセット」
                ret = Proc_SetPanel();
                if (ret != ProcResult.Success) break;

                // Step3 「裏面検査」
                ret = Proc_InspectionBack();
                if (ret != ProcResult.Success) break;

                // Step4 「パネル反転」
                ret = Proc_ReversePanel();
                if (ret != ProcResult.Success) break;
                
                // Step5 「表面検査」
                ret = Proc_InspectionFront();
                if (ret != ProcResult.Success) break;
            }

            // 終了
            Proc_Exit();

            return ret;
        }

        /// <summary>
        /// Step0 「初期化中」
        /// </summary>
        /// <returns>継続/エラー/終了</returns>
        ProcResult Proc_Intialize()
        {
            // 「初期化中」状態
            SetStep(InspectionStep.Intialize);
            Enum devtype;
            if (!m_devMgr.InitializeAll(out devtype))
            {
                // 機器初期化エラー
                SetErrorMsg($"機器初期化エラー({devtype})");
                return ProcResult.Error;
            }
            if (!m_devMgr.ConnectAll(out devtype))
            {
                // 機器接続エラー
                SetErrorMsg($"機器接続エラー({devtype})");
                return ProcResult.Error;
            }

            // PLCイベント設定
            m_devMgr.PLC.DetectError += OnPlcDetectError;
            m_devMgr.PLC.SignalChanged += OnPlcSetSignal;
            // PLC開始
            if (!m_devMgr.PLC.StartCommunication())
            {
                SetErrorMsg("PLC開始エラー");
                return ProcResult.Error;
            }
            // PLCエラー監視タスク開始
            m_devMgr.PLC.StartCheckTask();

            // パトライト：緑点灯
            if (!SetSignalTower(Device.SigType.Green))
            {
                return ProcResult.Error;
            }

            // 設備状態報告。選択したレシピ情報をCIMに通知する
            if (!UpdateRecipe())
            {
                SetErrorMsg("検査レシピ初期化中エラー");
                return ProcResult.Error;
            }

            // 終了待ち
            ProcResult pret = WaitFinish();
            return pret;
        }

        /// <summary>
        /// Step1 「ID取得」
        /// </summary>
        /// <returns>継続/エラー/終了</returns>
        ProcResult Proc_GetID()
        {
            // 画面に「ID取得」状態を設定
            SetStep(InspectionStep.GetId);

            // パトライト：黄点灯
            if (!SetSignalTower(Device.SigType.Yellow))
            {
                return ProcResult.Error;
            }
            
            // 画面の処理終了待ち
            ProcResult pret = WaitFinish();
            return pret;
        }

        /// <summary>
        /// Step2 「パネルセット」
        /// </summary>
        /// <returns>継続/エラー/終了</returns>
        ProcResult Proc_SetPanel()
        {
            // 画面に「パネルセット」状態を設定
            SetStep(InspectionStep.SetPanel);


            // 画面の処理終了待ち
            ProcResult pret = WaitFinish();
            return pret;
        }

        /// <summary>
        /// Step3 「裏面検査」
        /// </summary>
        /// <returns>継続/エラー/終了</returns>
        ProcResult Proc_InspectionBack()
        {
            // 画面に「搬入」状態を設定
            SetStep(InspectionStep.InspectionBack);

            // パトライト：緑点灯
            if (!SetSignalTower(Device.SigType.Green))
            {
                return ProcResult.Error;
            }

            // 画面の処理終了待ち
            ProcResult pret = WaitFinish();
            return pret;
        }

        /// <summary>
        /// Step4 「パネル反転」
        /// </summary>
        /// <returns>継続/エラー/終了</returns>
        ProcResult Proc_ReversePanel()
        {
            // 画面に「パネル反転」状態を設定
            SetStep(InspectionStep.ReversePanel);

            // パトライト：黄点灯
            if (!SetSignalTower(Device.SigType.Yellow))
            {
                return ProcResult.Error;
            }

            // 画面の処理終了待ち
            ProcResult pret = WaitFinish();
            return pret;
        }

        /// <summary>
        /// Step5 「表面検査」
        /// </summary>
        /// <returns>継続/エラー/終了</returns>
        ProcResult Proc_InspectionFront()
        {
            // 画面に「搬入」状態を設定
            SetStep(InspectionStep.InspectionFront);

            // パトライト：緑点灯
            if (!SetSignalTower(Device.SigType.Green))
            {
                return ProcResult.Error;
            }

            // 画面の処理終了待ち
            ProcResult pret = WaitFinish();
            return pret;
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        void Proc_Exit()
        {

        }

        /// <summary>
        /// シグナルタワー状態変更時に画面に通知
        /// </summary>
        /// <param name="list">各点灯状態ON/OFFのリスト</param>
        private void OnPlcSetSignal(Dictionary<Device.SigType, bool> list)
        {
            m_fmMain.PostMessage(FormMessages.UpdateSignalState(list));
        }

        /// <summary>
        /// PLCエラー検出時に画面に通知
        /// </summary>
        /// <param name="val">エラー値</param>
        /// <param name="text">エラーメッセージ</param>
        private void OnPlcDetectError(int val, string text)
        {
            m_fmMain.PostMessage(FormMessages.DetectError(val, text));
        }

        /// <summary>
        /// 検査結果のアップロード
        /// </summary>
        /// <param name="prm">パラメータ</param>
        /// <returns>成功/失敗</returns>
        bool OnActionUploadResult(UI.ActionParam_UploadResult prm)
        {
            string panelID = m_panelID;
            string modID = m_moduleID;
            int jude = prm.InspectionList.AllSuccess ? 0 : 1;
            string opeID = m_user.ID;
            string recipe = m_recipeNo;
            string prod_cd = prm.InspectionList.ItemCode;
            int fcode_mp01 = 0;

            bool bret;
            var rlist = new List<CIMManager.Common.InspectionResultParamBase>();
            // 検査データのリストをもとに、CIMの検査結果リストを生成
            foreach (var insItem in prm.InspectionList.Data.ItemList)
            {
                var p = new CIMManager.Common.InspectionResultParamAppearanceInspecion(prm.InspectionList.StartTime, insItem.Name, insItem.Result, insItem.ResultFCODE);
                rlist.Add(p);
                if (insItem.ResultFCODE != 0) fcode_mp01 = insItem.ResultFCODE;
            }

            // 検査結果報告アップロード
            bret = m_devMgr.CIM.SendReportDatas(panelID, rlist, prm.InspectionList.IsSurface);
            if (!bret)
            {
                log.Error("検査結果報告送信エラー");
            }

            // 実績報告をアップロード
            bret = m_devMgr.CIM.SendPerformanceReport(modID, prod_cd, recipe, /*★ランク*/"A", jude, fcode_mp01, opeID, m_chassisID, prm.InspectionList.StartTime, prm.InspectionList.EndTime, prm.InspectionList.IsSurface);
            if (!bret)
            {
                log.Error("実績報告送信エラー");
                return false;
            }

            // 印刷開始
            if (prm.InspectionList.IsSurface)
            {
                // 表面(=検査)終了時にラベルをプリント
                PrintID();
            }

            return bret;
        }

        /// <summary>
        /// レシピ情報をCIMに報告
        /// </summary>
        /// <returns>成功/失敗</returns>
        bool UpdateRecipe()
        {
            if (m_cfg.LastRecipeNo == m_recipeNo)
            {
                // 前回選択したレシピ番号と同じ → 処理なし
                return true;
            }

            log.Info("レシピNo更新({0}->{1})", m_cfg.LastRecipeNo, m_recipeNo);

            // 設備状態報告
            if (!m_devMgr.CIM.SetEquipmentStatus(m_recipeNo))
            {
                return false;
            }

            // レシピ番号更新
            m_cfg.LastRecipeNo = m_recipeNo;
            if (!m_cfg.SaveUserData())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 暫定：IDを印刷
        /// </summary>
        void PrintID()
        {
            Task.Run(() => {
                // 製品名・パネルID・モジュールID　をプリントアウト
                bool bret = m_devMgr.Printer.PrintOutID(m_insList.ProductName,
                    m_panelID,
                    m_moduleID,
                    m_insList.TemplateFileNameBarcode,
                    m_insList.PrintNumBarcode,
                    m_insList.TemplateFileNameText,
                    m_insList.PrintNumText);
                if (bret)
                {
                    log.Info("ラベル印刷 正常終了しました");
                }
                else
                {
                    log.Error("ラベル印刷 異常終了しました");
                }
            });
        }


   

        override protected void OnControllerAction(CommonUI.ActionParam_Base aParam)
        {
            base.OnControllerAction(aParam);

            if (aParam is UI.ActionParam_UploadResult)
            {
                aParam.result = OnActionUploadResult(aParam as UI.ActionParam_UploadResult);
            }

            if (aParam is UI.ActionParam_ChassisID)
            {
                var prm = aParam as UI.ActionParam_ChassisID;
                m_chassisID = prm.ChassisID;

                if (prm.PanelID == null)
                {
                    // CIM問い合わせ　★仕様不明
                    Task.Delay(1500).Wait();
                    prm.PanelID = "testPaaaaaa";
                    prm.ModuleID = "testMxxx";
                }

                m_moduleID = prm.ModuleID;
                m_panelID = prm.PanelID;
            }

            if (aParam is UI.ActionParam_ReadyInspecton)
            {
            }
        }


        /// <summary>
        /// 文字列化
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "コントローラー(検査)";
        }
    }

}
