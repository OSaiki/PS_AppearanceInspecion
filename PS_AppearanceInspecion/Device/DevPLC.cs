using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Dynamic;
using CommonLib;
using PLC;


namespace Device
{
    /// <summary>
    /// PLC
    /// </summary>
    class DevPLC : IDevice
    {
        /// <summary>
        /// ターゲット
        /// </summary>
        public string Target { get { return m_plc.GetTarget(); } }
        /// <summary>
        /// 通信終了時ハンドラ<br/>引数は正常切断時はnullとする
        /// </summary>
        public event Action<object> Closed = delegate { };

        Log log;
        /// <summary>
        /// 通信先
        /// </summary>
        KVN60AT m_plc;

        bool m_is_active = false;
        CancellationTokenSource m_cts;
        int m_detect_level = -1;

        /// <summary>
        /// シグナルタワー値設定時ハンドラ
        /// </summary>
        public event Action<Dictionary<SigType, bool>> SignalChanged = delegate { };

        /// <summary>
        /// PLCエラー検出時ハンドラ
        /// </summary>
        public event Action<int, string> DetectError = delegate { };
        /// <summary>
        /// 現在のシグナルタワー状態
        /// </summary>
        Dictionary<PLCAddress, bool> NowPlcState { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DevPLC()
        {
            log = new Log(this);
            m_plc = new KVN60AT();
            m_cts = new CancellationTokenSource();

            NowPlcState = new Dictionary<PLCAddress, bool>();
        }

        /// <summary>
        /// PLCに接続
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Connect()
        {
            return m_plc.Connect();
        }
        /// <summary>
        /// 初期化
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Initialize()
        {
            return m_plc.Initialize();
        }
        /// <summary>
        /// 終了
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Exit()
        {
            StopCommunication();
            return m_plc.Exit();
        }

        /// <summary>
        /// キャンセル
        /// </summary>
        public void Cancel()
        {
            Exit();
        }

        /// <summary>
        /// 設定値反映
        /// </summary>
        /// <param name="eoSetting">設定パラメータ</param>
        /// <returns>成功/失敗</returns>
        public bool SetParam(ExpandoObject eoSetting)
        {
            bool bret = m_plc.SetParam(eoSetting);
            if (bret)
            {
                m_plcsetting = new PLC_SettingParam();
                bret = m_plcsetting.SetParam(eoSetting);
            }
            return bret;
        }

        /// <summary>
        /// PLC通信開始
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool StartCommunication()
        {

            lock (this)
            {
                // 通信開始コマンド
                bool bret = m_plc.StartCommunication();
                if (!bret)
                {
                    log.Error("通信開始エラー");
                    return false;
                }
            }

            log.Info("通信開始");
            m_is_active = true;
            return true;
        }
        /// <summary>
        /// PLC通信終了
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool StopCommunication()
        {
            bool bret = true;
            if (m_is_active)
            {
                lock (this)
                {
                    // 通信終了コマンド
                    bret = m_plc.StopCommunication();
                }
                m_is_active = false;
                if (bret)
                {
                    log.Info("通信停止");
                }
                else
                {
                    log.Error("通信停止エラー");
                }
            }
            return bret;
        }


        /// <summary>
        /// シグナルタワー設定
        /// </summary>
        /// <param name="dic">シグナルとON/OFF状態のペア</param>
        /// <returns>成功/失敗</returns>
        private bool SetSignalTower(Dictionary<SigType, bool> dic)
        {
            // PLCAddressへの対応テーブル
            Dictionary<SigType, PLCAddress> convTbl = new Dictionary<SigType, PLCAddress>()
            {
                [SigType.Red]         = PLCAddress.PatoRed,
                [SigType.Yellow]      = PLCAddress.PatoYellow,
                [SigType.Green]       = PLCAddress.PatoGreen,
                [SigType.RedBlink]    = PLCAddress.PatoRedBlink,
                [SigType.YellowBlink] = PLCAddress.PatoYellowBlink,
                [SigType.GreenBlink]  = PLCAddress.PatoGreenBlink,
                [SigType.BuzCont]     = PLCAddress.BuzzerCont,
                [SigType.BuzInt]      = PLCAddress.BuzzerInt,
            };

           
            Dictionary<SigType, bool> report = new Dictionary<SigType, bool>();

            lock (this)
            {
                foreach (var pair in dic)
                {
                    PLCAddress plcaddr = convTbl[pair.Key];
                    bool bit = pair.Value;
                    if (!NowPlcState.ContainsKey(plcaddr) || NowPlcState[plcaddr] != bit)
                    {
                        // 現在の状態と差異がある

                        // PLCに書き込み
                        bool bret = m_plc.WriteBit(plcaddr, bit);
                        if (!bret)
                        {
                            return false;
                        }

                        // 現在の状態を更新
                        NowPlcState[plcaddr] = bit;
                        report.Add(pair.Key, bit);
                    }
                }
            }

            // 変化分を報告
            SignalChanged(report);

            return true;
        }

        /// <summary>
        /// シグナルタワー設定
        /// </summary>
        /// <param name="stype">点灯・鳴動パターン</param>
        /// <returns>成功/失敗</returns>
        public bool SignalTower(SigType stype)
        {
            var dic = new Dictionary<SigType, bool>()
            {
                [SigType.Red]         = stype.HasFlag(SigType.Red),
                [SigType.RedBlink]    = stype.HasFlag(SigType.RedBlink),
                [SigType.Yellow]      = stype.HasFlag(SigType.Yellow),
                [SigType.YellowBlink] = stype.HasFlag(SigType.YellowBlink),
                [SigType.Green]       = stype.HasFlag(SigType.Green),
                [SigType.GreenBlink]  = stype.HasFlag(SigType.GreenBlink),
                [SigType.BuzCont]     = stype.HasFlag(SigType.BuzCont),
                [SigType.BuzInt]      = stype.HasFlag(SigType.BuzInt)
            };

            if (m_detect_level >= 1)
            {
                dic.Remove(SigType.RedBlink);
            }

            bool bret = SetSignalTower(dic);

            if (bret)
            {
                log.Info($"シグナルタワー設定成功 [{stype}]");
            }
            else
            {
                log.Error($"シグナルタワー設定失敗 [{stype}]");
            }
            return bret;
        }

        /// <summary>
        /// PLCチェックタスク開始
        /// </summary>
        Task m_chkTask = null;
        /// <summary>
        /// PLCチェックタスク開始
        /// </summary>
        public void StartCheckTask()
        {
            // PLCチェックタスク
            m_chkTask = Task.Factory.StartNew(() => CheckTask(), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// PLC監視ループ
        /// </summary>
        void CheckTask()
        {
            while (true)
            {
                PLCAddress? addrE = null;
                PLCAddress? addrW = null;
                lock (this)
                {
                    if (!m_plc.CheckPLCErrorAI(out addrE) || !m_plc.CheckPLCWarning(out addrW))
                    {
                        log.Error("PLC監視時にエラー発生");
                        Closed("PLC監視時にエラー発生");
                        return;
                    }
                }

                if (addrE.HasValue && m_detect_level < 2)
                {
                    m_detect_level = 2;
                    log.Warning($"エラー'{addrE}'を検出");
                    DetectError(2, addrE.Value.ToDetailString());
                    // 赤点灯
                    SetSignalTower(new Dictionary<SigType, bool>() {[SigType.Red] = true });
                }
                else if (addrW.HasValue && m_detect_level < 1)
                {
                    m_detect_level = 1;
                    DetectError(1, addrW.Value.ToDetailString());
                    log.Warning($"警告'{addrW}'を検出");
                    // 赤点滅
                    SetSignalTower(new Dictionary<SigType, bool>() {[SigType.RedBlink] = true });
                }
                else if (m_detect_level < 0)
                {
                    m_detect_level = 0;
                    DetectError(0, null);
                }

                try
                {
                    int invmsec = m_plcsetting.ErrorCheckIntervalMin * 1000 * 60;
                    Task.Delay(invmsec).Wait(m_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    log.Info("PLCチェックタスクキャンセル");
                    break;
                }
                catch (Exception e)
                {
                    log.Error($"PLCチェックタスクエラー:{e}");
                    break;
                }
            }
        }

        /// <summary>
        /// 文字列化
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "PLC";
        }

        PLC_SettingParam m_plcsetting;
        /// <summary>
        /// PLC用設定パラメータクラス
        /// </summary>
        private class PLC_SettingParam : SettingParam
        {
            /// <summary>
            /// エラーチェック間隔（分単位指定）
            /// </summary>
            [May(30)]
            public int ErrorCheckIntervalMin { get; private set; }
        }
    }

    /// <summary>
    /// シグナルタワー設定種別
    /// </summary>
    [Flags]
    public enum SigType
    {
        /// <summary>
        /// 点灯無し（全消灯）
        /// </summary>
        None = 0,
        /// <summary>
        /// 赤点灯
        /// </summary>
        Red         = 1 << 0,
        /// <summary>
        /// 赤点滅
        /// </summary>
        RedBlink    = 1 << 1,
        /// <summary>
        /// 黄点灯
        /// </summary>
        Yellow      = 1 << 2,
        /// <summary>
        /// 黄点滅
        /// </summary>
        YellowBlink = 1 << 3,
        /// <summary>
        /// 緑点灯
        /// </summary>
        Green       = 1 << 4,
        /// <summary>
        /// 緑点滅
        /// </summary>
        GreenBlink  = 1 << 5,

        /// <summary>
        /// ブザー鳴動連続音
        /// </summary>
        BuzCont     = 1 << 6,
        /// <summary>
        /// ブザー鳴動連続音
        /// </summary>
        BuzInt      = 1 << 7,
    }



}
