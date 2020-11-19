using System;
using System.Dynamic;
using CommonLib;
using System.Collections.Generic;

namespace Device
{
    /// <summary>
    /// プリンターデバイス
    /// </summary>
    class DevPrinter : IDevice
    {
        /// <summary>
        /// ターゲット
        /// </summary>
        public string Target { get { return m_printer.PrinterName; } }
        /// <summary>
        /// プリンタ使用
        /// </summary>
        public bool Use
        {
            get { return m_printer.Use; }
            set { m_printer.Use = value; }
        }
        /// <summary>
        /// オブジェクト名
        /// </summary>
        public List<string> ObjectNameList
        {
            get
            {
                return new List<string>
                {
                    objNameKind,
                    objNamePanelID,
                    objNameModuleID,
                    objNameBarcode,
                };
            }
        }

        /// <summary>
        /// 通信終了時ハンドラ<br/>引数は正常切断時はnullとする
        /// </summary>
        public event Action<object> Closed = delegate { };


        Log log;
        Printer.LabelPrinter m_printer;

        const string objNameKind = "品種";
        const string objNamePanelID = "パネルID";
        const string objNameModuleID = "モジュールID";
        const string objNameBarcode = "バーコード";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DevPrinter()
        {
            log = new Log(this);
            m_printer = new Printer.LabelPrinter();
        }

        /// <summary>
        /// 設定値取得
        /// </summary>
        /// <param name="eo">設定パラメータ</param>
        /// <returns></returns>
        public bool SetParam(ExpandoObject eo)
        {
            if (m_printer.SetParam(eo))
            {
                return true;
            }
            else
            {
                log.Error("パラメータ設定失敗");
                return false;
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Initialize()
        {
            bool bret = true;
            bret = m_printer.Initialize();
            return bret;
        }

        /// <summary>
        /// 接続
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Connect()
        {
            bool bret = m_printer.Connect();
            return bret;
        }

        /// <summary>
        /// ID印刷
        /// </summary>
        /// <param name="prodID">製品ID</param>
        /// <param name="panelID">パネルID</param>
        /// <param name="modID">モジュールID</param>
        /// <param name="templateBarcode">テンプレートファイル(QR)</param>
        /// <param name="printNumQR">印刷枚数(QR)</param>
        /// <param name="templateText">テンプレートファイル(テキスト)</param>
        /// <param name="printNumText">印刷枚数(テキスト)</param>
        /// <returns>成功/失敗</returns>
        public bool PrintOutID(string prodID,
            string panelID,
            string modID,
            string templateBarcode,
            int printNumQR,
            string templateText,
            int printNumText)
        {
            // 設定
            string barcode = panelID + modID + " " + prodID;
            var dicParam = new Dictionary<string, string>();
            dicParam.Add(objNameKind, prodID);
            dicParam.Add(objNamePanelID, panelID);
            dicParam.Add(objNameModuleID, modID);
            dicParam.Add(objNameBarcode, barcode);

            // 印刷ジョブの設定開始 
            if (!m_printer.StartPrint(true, true))
            {
                return false;
            }

            // 印刷ジョブの追加
            if (!m_printer.PrintOut(templateBarcode, dicParam, printNumQR))
            {
                log.Error("QRコード印刷ジョブ追加に失敗");
                return false;
            }

            if (!m_printer.PrintOut(templateText, dicParam, printNumText))
            {
                log.Error("テキスト印刷ジョブ追加に失敗");
                return false;
            }

            // 印刷ジョブの設定終了(=印刷開始)
            if (!m_printer.EndPrint())
            {
                log.Error("印刷ジョブの設定終了(=印刷開始)に失敗");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 終了
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool Exit()
        {
            bool bret = m_printer.Exit();
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
        /// 文字列化
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "プリンター";
        }
    }
}
