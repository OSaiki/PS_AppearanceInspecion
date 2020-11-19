using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonUI;

namespace PS_AppearanceInspecion
{
    /// <summary>
    /// 画面への通知メッセージ
    /// </summary>
    class FormMessages
    {
        /// <summary>
        /// 検査ステップ更新
        /// </summary>
        public const int MSG_UPDATE_STEP = 11;
        /// <summary>
        /// シグナルタワー点灯状態更新
        /// </summary>
        public const int MSG_UPDATE_SIGNAL_STATE = 12;
        /// <summary>
        /// PLCエラー検出時
        /// </summary>
        public const int MSG_DETECT_ERROR = 13;

        /// <summary>
        /// 検査ステップ更新情報
        /// </summary>
        public class UpdateStepInfo
        {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="step">次の工程</param>
            public UpdateStepInfo(InspectionStep step)
            {
                Step = step;
                m_taskCmp = new TaskCompletionSource<bool>();
            }

            public InspectionStep Step { get; private set; }

            TaskCompletionSource<bool> m_taskCmp;

            /// <summary>
            /// 工程終了待機関数
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            public bool WaitFinish(CancellationToken ct)
            {
                m_taskCmp.Task.Wait(ct);
                return true;
            }

            /// <summary>
            /// 工程終了関数
            /// </summary>
            /// <param name="result"></param>
            public void Finish(bool result = true)
            {
                m_taskCmp.SetResult(result);
            }
        }
        /// <summary>
        /// 検査ステップ更新メッセージ
        /// </summary>
        /// <param name="step">検査ステップ更新情報</param>
        /// <returns>メッセージ</returns>
        static public FormMessage UpdateStep(UpdateStepInfo info)
        {
            return new FormMessage(MSG_UPDATE_STEP) { MsgData = info };
        }

        /// <summary>
        /// シグナルタワー点灯状態更新メッセージ
        /// </summary>
        /// <param name="statelist">シグナルタワー種別とON/OFFのペア情報</param>
        /// <returns>メッセージ</returns>
        static public FormMessage UpdateSignalState(Dictionary<Device.SigType, bool> statelist)
        {
            return new FormMessage(MSG_UPDATE_SIGNAL_STATE) { MsgData = statelist };
        }

        /// <summary>
        /// PLCエラー検出メッセージ
        /// </summary>
        /// <param name="lv">警告レベル</param>
        /// <param name="msg">警告内容</param>
        /// <returns>メッセージ</returns>
        static public FormMessage DetectError(int lv, string msg)
        {
            return new FormMessage(MSG_DETECT_ERROR) { MsgData = new Tuple<int, string>(lv, msg) };
        }
    }
}
