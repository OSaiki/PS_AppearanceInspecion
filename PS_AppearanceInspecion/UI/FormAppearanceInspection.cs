using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CommonLib;
using CommonUI;

namespace PS_AppearanceInspecion.UI
{
    /// <summary>
    /// 外観検査モードメイン画面
    /// </summary>
    partial class FormAppearanceInspection : FormBase
    {
        /// <summary>
        /// 検査位置
        /// </summary>
        int m_InspectionIndex = 0;

        Operator m_user;
        string m_recipeNo;
        Inspection.InspectionList m_InspectionList;

        FormInputChassisId m_formChassisID = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="recipeno">レシピ番号</param>
        /// <param name="user">作業者</param>
        /// <param name="lst">検査レシピリスト</param>
        public FormAppearanceInspection(string recipeno, Operator user, Inspection.InspectionList lst) : base("外観検査")
        {
            InitializeComponent();
            m_user = user;
            m_recipeNo = recipeno;
            m_InspectionList = lst;

            m_stepView.AddStep("初期化", InspectionStep.Intialize);
            m_stepView.AddStep("ID読み取り", InspectionStep.GetId);
            m_stepView.AddStep("パネルセット", InspectionStep.SetPanel);
            m_stepView.AddStep("裏面検査", InspectionStep.InspectionBack);
            m_stepView.AddStep("パネル反転", InspectionStep.ReversePanel);
            m_stepView.AddStep("表面検査", InspectionStep.InspectionFront);

            tBOperatorID.Text = m_user.ID;
            tBInspectionProgramFile.Text = m_recipeNo;
            m_InspectionList = lst;

            FormClosing += OnFormClosing;
        }

        /// <summary>
        /// 右上の閉じる[x]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (Result == FormResult.None)
            {
                if (!MsgBox.Inquiry("検査途中です。結果を送信せずに終了しますか？"))
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    log.UI("閉じる[X]ボタン");
                    Result = FormResult.Cancel;
                }
            }
        }

        /// <summary>
        /// 現在のステップ（工程）
        /// </summary>
        InspectionStep m_InspectionStep = InspectionStep.None;

        /// <summary>
        /// 工程のステップ（工程）
        /// </summary>
        /// <param name="state">新しい工程</param>
        async void SetStep(InspectionStep state)
        {
            log.UI("状態更新 {0} -> {1}", m_InspectionStep, state);
            m_InspectionStep = state;
            m_stepView.UpdateState(state);

            switch (state)
            {
                // 初期化
                case InspectionStep.Intialize:
                    FinishStep(true);
                    break;
                // ID取得
                case InspectionStep.GetId:
                    bool bret = await GetIDLoop();
                    if (!bret)
                    {
                        Result = FormResult.Cancel;
                        Close();
                        return;
                    }
                    FinishStep(true);
                    break;
                // パネルセット
                case InspectionStep.SetPanel:
                    if (!MsgBox.ContinueConfirm("パネルをセットして下さい(裏面)"))
                    {
                        Result = FormResult.Cancel;
                        Close();
                        return;
                    }
                    await CallControllerAction(new ActionParam_ReadyInspecton());
                    FinishStep(true);
                    break;
                // 裏面検査
                case InspectionStep.InspectionBack:
                    StartInspection(Inspection.ISP_Mode.BackSurface);
                    break;
                // パネル反転
                case InspectionStep.ReversePanel:
                    if (!MsgBox.ContinueConfirm("表面を検査します。パネルを反転して下さい"))
                    {
                        Result = FormResult.Cancel;
                        Close();
                        return;
                    }
                    FinishStep(true);
                    break;
                // 表面検査
                case InspectionStep.InspectionFront:
                    StartInspection(Inspection.ISP_Mode.Surface);
                    break;
            }
        }

        /// <summary>
        /// 検査開始
        /// </summary>
        /// <param name="side">モード(表or裏)</param>
        void StartInspection(Inspection.ISP_Mode side)
        {
            m_InspectionIndex = 0;
            m_InspectionList.Start(side);
            SetInspection();
        }

        /// <summary>
        /// 検査終了
        /// </summary>
        async void EndInspection()
        {
            btnOK.Image = m_InspectionList.Data.ispResult == Inspection.ISP_State.OK ? CheckIcon : null;
            btnNG.Image = m_InspectionList.Data.ispResult == Inspection.ISP_State.NG ? CheckIcon : null;

            // 検査リストから結果画面表示用のパラメータを作成
            List<FormResultCheck.ResultItem> lst = new List<FormResultCheck.ResultItem>() { new FormResultCheck.ResultItem(0, m_InspectionList.AllSuccess, m_InspectionList.Data.GetResultString()) };

            string title = m_InspectionList.IsSurface ? "検査結果<表面>" : "検査結果<裏面>";
            // 検査結果画面生成
            FormResultCheck fmResult = new FormResultCheck(lst, title, false);
            // 検査結果画面表示
            var dret = fmResult.ShowDialog();

            if (fmResult.Result == FormResult.Cancel)
            {
                // 検査終了
                Result = FormResult.Cancel;
                Close();
                return;
            }

            if (fmResult.ReInspectionIdx >= 0)
            {
                // 再検査を行う場合
                m_InspectionIndex = fmResult.ReInspectionIdx;
                SetInspection();
                return;
            }
            else
            {
                m_InspectionList.End();
                // 結果送信
                var prm = new ActionParam_UploadResult() { InspectionList = m_InspectionList };
                var ret = await CallControllerAction(prm);

                if (!ret)
                {
                    // エラー
                    MsgBox.Error("結果送信時にエラーが発生しました");
                    Result = FormResult.Exit;
                    Close();
                    return;
                }
                ClearButtons();

                if (m_InspectionStep == InspectionStep.InspectionFront)
                {
                    if (!MsgBox.ContinueConfirm("続けて他のパネルも検査する場合は、「次へ」を押下して下さい。"))
                    {
                        Result = FormResult.Cancel;
                        Close();
                        return;
                    }
                }

                // 次のステップへ
                FinishStep(true);
                return;
            }
        }




        List<InsButton> m_InsButtons = new List<InsButton>();

        void ClearButtons()
        {
            foreach (InsButton b in m_InsButtons)
            {
                m_flpInspection.Controls.Remove(b);
                b.Dispose();
            }
            m_InsButtons.Clear();
        }

        /// <summary>
        /// 現在の検査データに即した画面を設定する
        /// </summary>
        void SetInspection()
        {
            Inspection.InspectionData d = m_InspectionList.Data;

            ClearButtons();

            m_flpInspection.Font = new Font("MS UI Gothic", 18);


            foreach (var item in d.ItemList)
            {
                InsButton btn = new InsButton(item, OnInsItemClick);
                m_InsButtons.Add(btn);
                m_flpInspection.SetFlowBreak(btn, item.Newline);
                m_flpInspection.Controls.Add(btn);
            }
            OnInsItemClick();

            btnOK.Image = d.ispResult == Inspection.ISP_State.OK ? CheckIcon : null;
            btnNG.Image = d.ispResult == Inspection.ISP_State.NG ? CheckIcon : null;
        }

        /// <summary>
        /// 検査項目ボタン
        /// </summary>
        class InsButton : Button
        {
            Inspection.InspectionItem m_item;
            public InsButton(Inspection.InspectionItem item, Action callBack)
            {
                m_item = item;

                Text = m_item.Name;
                MinimumSize = new Size(170, 60);
                AutoSize = true;
                Margin = new Padding(8);
                ImageAlign = ContentAlignment.TopLeft;
                Click += (_s,_e)=> { SetState(!m_item.NG); callBack(); };
                SetState(m_item.NG);
            }

            void SetState(bool ng)
            {
                m_item.NG = ng;
                if (!ng)
                {
                    Image = null;
                    BackColor = Color.Transparent;
                }
                else
                {
                    Image = NGIcon;
                    BackColor = Color.FromArgb(40, 255, 0, 0);
                }
            }
        }

        /// <summary>
        /// ボタン押下時に結果ボタンの有効/無効を更新
        /// </summary>
        private void OnInsItemClick()
        {
            // NGが1個でもあるかどうか
            if (!m_InspectionList.AllSuccess)
            {
                btnOK.Enabled = false;
                btnNG.Enabled = true;
            }
            else
            {
                btnOK.Enabled = true;
                btnNG.Enabled = false;
            }
        }

        /// <summary>
        /// ID取得
        /// </summary>
        /// <returns></returns>
        async Task<bool> GetIDLoop()
        {
            while (true)
            {
                // シャーシID入力画面を表示する
                using (FormInputChassisId fm = new FormInputChassisId(true))
                {
                    m_formChassisID = fm;
                    fm.StartPosition = FormStartPosition.CenterParent;
                    fm.ShowDialog();
                    m_formChassisID = null;
                    if (fm.Result == FormResult.Cancel)
                    {
                        Result = FormResult.Cancel;
                        Close();
                        return false;
                    }
                    // シャーシID設定
                    tBChassisID.Text = fm.ChassisID;

                    // IDを問い合わせ
                    var prm = new ActionParam_ChassisID(fm.ChassisID);
                    if (fm.ModuleID != null)
                    {
                        // ★暫定仕様★　ポップアップでモジュールID＋パネルIDも入力してある場合
                        prm.ModuleID = fm.ModuleID;
                        prm.PanelID = fm.PanelID;
                    }

                    await CallControllerAction(prm);

                    if (prm.result)
                    {
                        tBModuleID.Text = prm.ModuleID;
                        tBPanelID.Text = prm.PanelID;
                        break;
                    }

                    MsgBox.Warning("ID'{0}'に対応するパネルが見つかりませんでした", fm.ChassisID);
                }
            }
            return true;
        }

        protected override void OnInputKey(string str)
        {
            if (m_formChassisID != null)
            {
                m_formChassisID.OnInputID(str);
            }
        }

        /// <summary>
        /// 外部からのメッセージ
        /// </summary>
        /// <param name="msg">メッセージ</param>
        protected override void OnFormMessage(FormMessage msg)
        {
            switch (msg.MsgType)
            {
                case FormMessages.MSG_UPDATE_STEP:
                    var updMsg = msg.MsgData as FormMessages.UpdateStepInfo;
                    FinishStep = updMsg.Finish;
                    SetStep(updMsg.Step);
                    break;

                case FormMessages.MSG_UPDATE_SIGNAL_STATE:
                    var sig_states = msg.MsgData as Dictionary<Device.SigType, bool>;
                    foreach (var pair in sig_states)
                    {
                        switch (pair.Key)
                        {
                            case Device.SigType.Red:          m_SignalView.R      = pair.Value; break;
                            case Device.SigType.RedBlink:     m_SignalView.RBlink = pair.Value; break;
                            case Device.SigType.Yellow:       m_SignalView.Y      = pair.Value; break;
                            case Device.SigType.YellowBlink:  m_SignalView.YBlink = pair.Value; break;
                            case Device.SigType.Green:        m_SignalView.G      = pair.Value; break;
                            case Device.SigType.GreenBlink:   m_SignalView.GBlink = pair.Value; break;
                        }
                    }
                    break;

                case FormMessages.MSG_DETECT_ERROR:
                    var msgDE = msg.MsgData as Tuple<int, string>;
                    int errLevel = msgDE.Item1;
                    string errMsg = msgDE.Item2;
                    switch (errLevel)
                    {
                        case 0:
                            tBSochiState.Text = "異常無し";
                            break;
                        case 1:
                            tBSochiState.Text = "警報:" + errMsg;
                            tBSochiState.ForeColor = Color.Red;
                            tBSochiState.BackColor = Color.Yellow;
                            break;
                        case 2:
                            tBSochiState.Text = "エラー発生";
                            tBSochiState.ForeColor = Color.Red;
                            tBSochiState.BackColor = Color.Yellow;
                            MsgBox.Error("PLCでエラーが発生しました。終了します\n" + errMsg);
                            Result = FormResult.Exit;
                            Close();
                            break;
                    }
                    break;
            }

            base.OnFormMessage(msg);
        }

        Action<bool> FinishStep = null;

        /// <summary>
        /// 検査結果入力
        /// </summary>
        /// <param name="result">OK/NG/保留</param>
        void SetInspectionResult(Inspection.ISP_State result)
        {
            log.UI($"結果入力 Result={result}");
            m_InspectionList.Data.ispResult = result;
            m_InspectionList.Data.InsTime = DateTime.Now;

            // 結果画面表示、結果送信
            EndInspection();
        }


        /// <summary>
        /// OKボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            SetInspectionResult(Inspection.ISP_State.OK);
        }

        /// <summary>
        /// NGボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNG_Click(object sender, EventArgs e)
        {
            SetInspectionResult(Inspection.ISP_State.NG);
        }

        /// <summary>
        /// 保留ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnKeep_Click(object sender, EventArgs e)
        {
            SetInspectionResult(Inspection.ISP_State.Keep);
        }

        /// <summary>
        /// 戻るボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevious_Click(object sender, EventArgs e)
        {
            m_InspectionIndex--;
            SetInspection();
        }
        /// <summary>
        /// 次へボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNext_Click(object sender, EventArgs e)
        {
            m_InspectionIndex++;
            SetInspection();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {

        }
    }
    class ActionParam_UploadResult : CommonUI.ActionParam_Base
    {
        public Inspection.InspectionList InspectionList { set; get; }
    }

    /// <summary>
    /// シャーシID入力完了
    /// </summary>
    class ActionParam_ChassisID : ActionParam_Base
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="csid"></param>
        public ActionParam_ChassisID(string csid)
        {
            ChassisID = csid;
        }
        /// <summary>
        /// シャーシID
        /// </summary>
        public string ChassisID { set; get; }
        /// <summary>
        /// パネルID
        /// </summary>
        public string PanelID { set; get; }
        /// <summary>
        /// モジュールID
        /// </summary>
        public string ModuleID { set; get; }
    }

    class ActionParam_ReadyInspecton : CommonUI.ActionParam_Base
    {
    }

}
