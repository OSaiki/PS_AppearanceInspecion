using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Dynamic;
using System.Threading.Tasks;

namespace Inspection
{


    /// <summary>
    /// 検査の種類
    /// </summary>
    enum ISP_Mode
    {
        /// <summary>
        /// 裏面検査
        /// </summary>
        BackSurface,
        /// <summary>
        /// 表面検査
        /// </summary>
        Surface
    }

    /// <summary>
    /// 検査のResult
    /// </summary>
    enum ISP_State
    {
        /// <summary>
        /// 未検査
        /// </summary>
        Undecided,
        /// <summary>
        /// 検査OK
        /// </summary>
        OK,
        /// <summary>
        /// 検査NG
        /// </summary>
        NG,
        /// <summary>
        /// 保留
        /// </summary>
        Keep
    }

    /// <summary>
    /// 外観検査レシピ設定データ
    /// </summary>
    class InspectionRecipeSetting : CommonLib.SettingParam
    {
        /// <summary>
        /// 品目コード
        /// </summary>
        [Must()]
        public string ItemCode { get; private set; }

        /// <summary>
        /// 製品名(プリンタで使用)
        /// </summary>
        [Must]
        public string ProductName { get; private set; }

        /// <summary>
        /// プリンタで使用するバーコード設定
        /// </summary>
        [May(null)]
        public string PrinterBarcodeSetting { get; private set; }
        /// <summary>
        /// プリンタで使用するテキスト設定
        /// </summary>
        [May(null)]
        public string PrinterTextSetting { get; private set; }

        /// <summary>
        /// 裏面検査レシピファイル
        /// </summary>
        [Must()]
        public string RecipeB { get; private set; }
        /// <summary>
        /// 表面検査レシピファイル
        /// </summary>
        [Must()]
        public string RecipeF { get; private set; }
    }

    /// <summary>
    /// 検査項目データ
    /// </summary>
    class InspectionItemSetting : CommonLib.SettingParam
    {
        /// <summary>
        /// 検査要素(複数指定)
        /// </summary>
        public List<string> Inspection { get; private set; }
    }

    /// <summary>
    /// 検査番号ごとの持ち物
    /// </summary>
    class InspectionData
    {
        public int inspectionNumber = 0;
        public string inspectionSummary = "";

        public string ispContents = "";
        public string ispStandard = "";

        public int ispItem = 0;

        public ISP_State ispResult;
        public int ErrorCode = 0;

        public DateTime InsTime = default(DateTime);

        public List<InspectionItem> ItemList { get; set; }

        /// <summary>
        /// 検査結果テキスト化
        /// </summary>
        /// <returns>テキスト</returns>
        public string GetResultString()
        {
            string text = "";
            int idx = 1;
            foreach (var item in ItemList)
            {
                string addText = $"検査項目{idx}: {item.Name}= {item.Result}";
                text += addText + "\r\n";
                idx++;
            }
            return text;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            // 検査結果を初期状態に戻す
            ispResult = ISP_State.Undecided;
            foreach (var item in ItemList)
            {
                item.NG = false;
            }
        }
    }

    /// <summary>
    /// 検査アイテム
    /// </summary>
    class InspectionItem
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">検査項目名</param>
        /// <param name="fc">対応する不良コード</param>
        public InspectionItem(string name, int fc, bool newline)
        {
            Name = name;
            FCode = fc;
            Newline = newline;
            NG = false;
        }

        public string Name { get; private set; }
        public int FCode { get; private set; }
        public bool Newline { get; private set; }
        public bool NG { get; set; }

        public string Result { get { return NG ? "NG" : "OK"; } }

        /// <summary>
        /// アップロードする不良コード。NGのみ設定値を返す
        /// </summary>
        public int ResultFCODE { get { return NG ? FCode : 0; } }
    }

    /// <summary>
    /// 検査リストクラス
    /// </summary>
    class InspectionList
    {
        /// <summary>
        /// 品目コード
        /// </summary>
        public string ItemCode { get; private set; }

        /// <summary>
        /// 製品名(プリンタで使用)
        /// </summary>
        public string ProductName { get; private set; }
        /// <summary>
        /// プリンタで使用するテンプレートファイル名(バーコード)
        /// </summary>
        public string TemplateFileNameBarcode { get; private set; }
        /// <summary>
        /// プリンタで使用する印刷数(バーコード)
        /// </summary>
        public int PrintNumBarcode { get; private set; }
        /// <summary>
        /// プリンタで使用するテンプレートファイル名(テキスト)
        /// </summary>
        public string TemplateFileNameText { get; private set; }
        /// <summary>
        /// プリンタで使用する印刷数(テキスト)
        /// </summary>
        public int PrintNumText { get; private set; }
        /// <summary>
        /// 検査の全てがOKか
        /// </summary>
        public bool AllSuccess
        {
            get { return Data.ItemList.All(item => item.NG == false); }
        }

        /// <summary>
        /// 外観検査レシピファイル読み込み
        /// </summary>
        /// <param name="fpath">ファイルパス</param>
        /// <returns>検査リスト</returns>
        public static InspectionList LoadRecipeFile(string fpath)
        {
            log.Info($"外観検査レシピファイル '{fpath}' 読み込み開始");
            InspectionRecipeSetting setting = new InspectionRecipeSetting();
            ExpandoObject eo = CommonLib.SettingFile.LoadSettingFile(fpath);
            if (!setting.SetParam(eo))
            {
                log.Error($"外観検査レシピファイルリードエラー");
                return null;
            }

            InspectionList ret = new InspectionList();
            ret.ItemCode = setting.ItemCode;
            ret.ProductName = setting.ProductName;

            // プリンタ設定
            if (!ParsePrinterSetting(setting.PrinterBarcodeSetting, setting.PrinterTextSetting, ret))
            {
                log.Error("プリンタ設定エラー");
                return null;
            }

            string dir = Path.GetDirectoryName(fpath);
            ret.m_ispDataFront = LoadInspectionItemFile(Path.Combine(dir, setting.RecipeF));
            if (ret.m_ispDataFront == null)
            {
                log.Error($"表面の検査項目ファイルが読み込めませんでした");
                return null;
            }

            ret.m_ispDataBack = LoadInspectionItemFile(Path.Combine(dir, setting.RecipeB));
            if (ret.m_ispDataBack == null)
            {
                log.Error($"裏面の検査項目ファイルが読み込めませんでした");
                return null;
            }

            log.Info($"外観検査レシピファイル '{fpath}' 読み込み完了");

            return ret;
        }

        /// <summary>
        /// プリンタ設定解析
        /// </summary>
        /// <param name="QRSetting">QRコード設定</param>
        /// <param name="textSetting">テキスト設定</param>
        /// <param name="inslist">検査リストクラス</param>
        /// <returns>成功/失敗</returns>
        static bool ParsePrinterSetting(string QRSetting, string textSetting, InspectionList inslist)
        {
            if (string.IsNullOrEmpty(QRSetting)
            && string.IsNullOrEmpty(textSetting))
            {
                // 設定なし(プリンタを使用しない)
                inslist.TemplateFileNameBarcode = null;
                inslist.TemplateFileNameText = null;
                inslist.PrintNumBarcode = 0;
                inslist.PrintNumText = 0;
            }
            else if (!string.IsNullOrEmpty(QRSetting)
            && !string.IsNullOrEmpty(textSetting))
            {
                // 設定あり(プリンタを使用する)
                var prmsBarcode = QRSetting.Split(':');
                var prmsText = textSetting.Split(':');
                if (prmsBarcode.Length == 2
                    && prmsText.Length == 2)
                {
                    int printNumBarcode = 0;
                    int printNumText = 0;

                    // 値のチェック(テンプレート)
                    if (string.IsNullOrWhiteSpace(prmsBarcode[0])
                        || string.IsNullOrWhiteSpace(prmsText[0]))
                    {
                        log.Error($"プリンタ設定のテンプレート設定が不正です。({prmsBarcode[0]}, {prmsText[0]})");
                        return false;
                    }
                    // 値のチェック(枚数)
                    if (!int.TryParse(prmsBarcode[1], out printNumBarcode)
                     & !int.TryParse(prmsText[1], out printNumText))
                    {
                        log.Error($"プリンタ設定の印刷枚数設定が不正です。({prmsBarcode[1]}, {prmsText[1]})");
                        return false;
                    }
                    inslist.TemplateFileNameBarcode = prmsBarcode[0];
                    inslist.TemplateFileNameText = prmsText[0];
                    inslist.PrintNumBarcode = printNumBarcode;
                    inslist.PrintNumText = printNumText;
                }
                else
                {
                    log.Error($"プリンタ設定のフォーマットが不正です。コロンの数を確認して下さい");
                    return false;
                }
            }
            else
            {
                // QRコードまたはテキストの片方のみ設定されている（不正）
                log.Error($"プリンタ設定が不正です。QRコードとテキストが設定されているか確認して下さい");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 検査項目設定ファイル読み込み
        /// </summary>
        /// <param name="fpath">ファイルパス</param>
        /// <returns>検査データ</returns>
        static InspectionData LoadInspectionItemFile(string fpath)
        {
            log.Info($"検査項目ファイル '{fpath}' 読み込み開始");
            InspectionItemSetting setting = new InspectionItemSetting();
            ExpandoObject eo = CommonLib.SettingFile.LoadSettingFile(fpath);
            if (!setting.SetParam(eo))
            {
                log.Error($"検査項目ファイルリードエラー");
                return null;
            }
            if (setting.Inspection.Count == 0)
            {
                log.Error($"検査項目ファイル内に有効な検査項目が存在しません");
                return null;
            }

            InspectionData data = new InspectionData();
            data.ItemList = new List<InspectionItem>();

            foreach (string val in setting.Inspection)
            {
                var splitVal = val.Split(',');
                if(splitVal.Length != 2 && splitVal.Length != 3)
                {
                    log.Error($"Inspectionタグ内書式エラー'{val}'");
                    return null;
                }
                else
                {
                    int fcode = 0;
                    string name = null;
                    bool newline = false;

                    // 「数字」のフォーマットに適合しているか正規表現で判定
                    var mc = Regex.Match(splitVal[0], @"^(\d+)$");
                    if (!mc.Success)
                    {
                        log.Error($"Inspectionタグ内書式エラー'{val}'");
                        return null;
                    }

                    string fcval = mc.Groups[1].Value;
                    if (!int.TryParse(fcval, out fcode) || fcode < 1 || fcode > 9999)
                    {
                        log.Error($"不良コード指定エラー'{fcval}'");
                        return null;
                    }

                    // 「任意の文字列」のフォーマットに適合しているか正規表現で判定
                    mc = Regex.Match(splitVal[1], @"^(.+)$");
                    if (!mc.Success)
                    {
                        log.Error($"Inspectionタグ内書式エラー'{val}'");
                        return null;
                    }
                    name = mc.Groups[1].Value;

                    if(splitVal.Length == 3)
                    {
                        // 「改行」と一致しているか判定
                        if (splitVal[2].Equals("改行"))
                        {
                            newline = true;
                        }
                        else
                        {
                            log.Error($"Inspectionタグ内書式エラー'{val}'");
                            return null;
                        }
                    }

                    InspectionItem item = new InspectionItem(name, fcode, newline);
                    data.ItemList.Add(item);

                }
            }
            log.Info($"検査項目ファイル '{fpath}' 読み込み完了 項目数={data.ItemList.Count}");
            return data;
        }

        ISP_Mode m_side;
        static CommonLib.Log log = new CommonLib.Log("検査List");
        /// <summary>
        /// 裏面検査データ
        /// </summary>
        InspectionData m_ispDataBack;
        /// <summary>
        /// 表検査データ
        /// </summary>
        InspectionData m_ispDataFront;

        /// <summary>
        /// 検査データ
        /// </summary>
        public InspectionData Data
        {
            get { return IsSurface ? m_ispDataFront : m_ispDataBack; }
        }

        /// <summary>
        /// 表面かどうか
        /// </summary>
        public bool IsSurface
        {
            get { return m_side == ISP_Mode.Surface; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private InspectionList()
        {
        }

        /// <summary>
        /// 検査開始
        /// </summary>
        /// <param name="side">検査種別(表面検査/裏面検査)</param>
        public void Start(ISP_Mode side)
        {
            m_side = side;
            Data.Initialize();
            StartTime = DateTime.Now;
            log.Info($"開始({m_side})");
        }
        /// <summary>
        /// 検査終了
        /// </summary>
        public void End()
        {
            EndTime = DateTime.Now;
            log.Info($"終了({m_side})");
        }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }


    }
}

