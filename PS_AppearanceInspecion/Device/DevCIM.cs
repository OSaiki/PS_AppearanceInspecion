using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using CIMManager;
using CommonLib;

namespace Device
{
    /// <summary>
    /// CIM制御
    /// </summary>
    class DevCIM : IDevice
    {
        /// <summary>
        /// ターゲット
        /// </summary>
        public string Target { get { return m_cimMgr.GetServerPath(); } }
        /// <summary>
        /// 通信終了時ハンドラ<br/>引数は正常切断時はnullとする
        /// </summary>
        public event Action<object> Closed = delegate { };

        CIMManager.CIMManager m_cimMgr;
        CIM_SettingParam m_cimsetting;
        Log log;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DevCIM()
        {
            log = new Log(this);
            m_cimMgr = new CIMManager.CIMManager();
        }
        /// <summary>
        /// 設定値取得
        /// </summary>
        /// <param name="eo">設定パラメータ</param>
        /// <returns>成功/失敗</returns>
        public bool SetParam(ExpandoObject eo)
        {
            bool bret = m_cimMgr.SetParam(eo);
            if (bret)
            {
                m_cimsetting = new CIM_SettingParam();
                bret = m_cimsetting.SetParam(eo);
            }
            return bret;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Initialize()
        {
            bool bret = m_cimMgr.Initialize();
            return bret;
        }

        /// <summary>
        /// 接続
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Connect()
        {
            bool bret;

            if (!IsConnect())
            {
                log.Warning("CIMに接続できません");
            }

            // データ送信タスク起動
            bret = m_cimMgr.StartServer();
            return bret;
        }

        /// <summary>
        /// 接続確認
        /// </summary>
        /// <returns>true:接続状態 / false:接続失敗</returns>
        public bool IsConnect()
        {
            bool isExist;
            // ルートディレクトリが存在するかで判定
            if (!m_cimMgr.ExistDirectory("", out isExist))
            {
                return false;
            }
            else
            {
                if (isExist)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 終了
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Exit()
        {
            bool bret = true;
            // データ送信タスク停止
            bret = m_cimMgr.StopServer();
            return bret;
        }

        /// <summary>
        /// キャンセル
        /// </summary>
        public void Cancel()
        {
            Exit();
        }

        /// <summary>
        /// 実績報告(外観検査)
        /// </summary>
        /// <param name="modID">モジュールID</param>
        /// <param name="PROD_CD">品目コード</param>
        /// <param name="recipeNo">レシピ番号</param>
        /// <param name="_rankode">ランクコード</param>
        /// <param name="_jd">パネル良否</param>
        /// <param name="_fcode">不良コード</param>
        /// <param name="opeID">作業者ID</param>
        /// <param name="chassis">カセット</param>
        /// <param name="st">開始日時</param>
        /// <param name="ed">終了日時</param>
        /// <param name="side">パネル表/裏</param>
        /// <returns>成功/失敗</returns>
        public bool SendPerformanceReport(string modID, string PROD_CD, string recipeNo, string _rankode, int _jd, int _fcode, string opeID, string chassis, DateTime st, DateTime ed, bool side = true)
        {
            bool bret = true;
            string eqcd = GetEqCode(side);

            // 実績報告(MP01)ファイルの作成と送信
            bret = m_cimMgr.UploadReportData(
                eqcd,       // 設備コード
                modID,      // 製品ID（モジュールID）
                PROD_CD,    // 品目コード
                recipeNo,   // レシピ
                _rankode,   // ランク
                _jd,        // パネル良否
                _fcode,     // 不良コード
                opeID,      // 作業者ID
                chassis,    // カセット(簡易シャーシID)
                st,         // 開始日時
                ed,         // 終了日時
                true);      // モジュールID指定時のフラグ

            return bret;
        }

        /// <summary>
        /// 検査結果報告
        /// </summary>
        /// <param name="panelID">パネルID</param>
        /// <param name="rlist">検査結果リスト</param>
        /// <param name="side">パネル表/裏</param>
        /// <returns>成功/失敗</returns>
        public bool SendReportDatas(string panelID, List<CIMManager.Common.InspectionResultParamBase> rlist, bool side = true)
        {
            bool bret = true;
            string eqcd = GetEqCode(side);
            // 出荷前用検査結果報告(MP21)ファイルの作成と送信
            bret = m_cimMgr.UploadInspectionResultPS(eqcd, DateTime.Now, panelID, rlist);

            return bret;
        }


        /// <summary>
        /// 設備状態報告
        /// </summary>
        /// <param name="recipeno">レシピ番号</param>
        /// <returns>成功/失敗</returns>
        public bool SetEquipmentStatus(string recipeno)
        {
            string eqcd = GetEqCode();
            bool bret = m_cimMgr.UploadEquipmentStatusData(eqcd, recipeno);

            if (bret)
            {
                log.Info($"設備状態報告(ME11)完了 : レシピ番号={recipeno}");
            }
            else
            {
                log.Error($"設備状態報告(ME11)エラー : レシピ番号={recipeno}");
            }

            return bret;
        }
        /// <summary>
        /// 設備異常報告
        /// </summary>
        /// <param name="errcd">エラーコード</param>
        /// <param name="sts">設備状態</param>
        /// <param name="errmsg">エラー文言</param>
        /// <returns>成功/失敗</returns>
        public bool SetEquipmentError(string errcd, EquipmentStatusKind sts, string errmsg)
        {
            string eqcd = GetEqCode();
            bool bret = m_cimMgr.UploadEquipmentErrorData(eqcd, errcd, sts, errmsg);

            if (bret)
            {
                log.Info($"設備異常報告(ME01)完了 : 設備状態={sts}({(int)sts})");
            }
            else
            {
                log.Error($"設備異常報告(ME01)エラー : 設備状態={sts}({(int)sts})");
            }

            return bret;
        }

        /// <summary>
        /// 接続するFTPサーバーを変更する
        /// </summary>
        /// <param name="server_path">サーバーパス</param>
        /// <param name="ftp_user">ユーザー名</param>
        /// <param name="ftp_password">パスワード</param>
        /// <returns>成功/失敗</returns>
        public bool ChangeConnectionFTPServer(string server_path, string ftp_user, string ftp_password)
        {
            try
            {
                if (!m_cimMgr.ChangeConnectionFTPServer(server_path, ftp_user, ftp_password))
                {
                    log.Error("FTPサーバー変更失敗");
                    return false;
                }
                else
                {
                    log.Info("FTPサーバー変更成功");
                    return true;
                }
            }
            catch (Exception e)
            {
                log.Error("FTPサーバー変更失敗 : " + e);
                return false;
            }
        }

        /// <summary>
        /// 未送信データリストを設定
        /// </summary>
        /// <param name="UnsendDataList">未送信データリスト</param>
        public void SetUnsendDataList(List<CIMManager.Common.FTPServerQueData> UnsendDataList)
        {
            m_cimMgr.SetUnsendDataList(UnsendDataList);
        }

        /// <summary>
        /// 未送信データリストを取得
        /// </summary>
        /// <returns>未送信データリスト</returns>
        public List<CIMManager.Common.FTPServerQueData> GetUnsendDataList()
        {
            return m_cimMgr.GetUnsendDataList();
        }


        /// <summary>
        /// 文字列化(ログ用)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "CIM";
        }

        /// <summary>
        /// 設備コード取得
        /// </summary>
        /// <param name="side">表/裏</param>
        /// <returns>設備コード設定値</returns>
        private string GetEqCode(bool side = true)
        {
            return side ? m_cimsetting.EQCD : m_cimsetting.EQCD_B;
        }

        /// <summary>
        /// 外観検査CIM用設定パラメータクラス
        /// </summary>
        private class CIM_SettingParam : SettingParam
        {
            /// <summary>
            /// 設備コード。必須パラメータ
            /// </summary>
            [Must]
            public string EQCD { get; private set; }
            /// <summary>
            /// 裏面設備コード。必須パラメータ
            /// </summary>
            [Must]
            public string EQCD_B { get; private set; }
        }
    }
}
