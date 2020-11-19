using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Dynamic;
using CommonLib;

namespace Config
{
    /// <summary>
    /// 設定データ管理<br/>XML設定ファイルの操作や設定値の保持を行う。
    /// </summary>
    class ConfigDataManager : ConfigBase
    {
        /// <summary>
        /// アプリの設定ファイルを読み込み、自クラスのインスタンスを返す
        /// </summary>
        /// <param name="cfgFileName">アプリ設定ファイル名</param>
        /// <param name="errorStr">リード失敗時のエラー情報</param>
        /// <returns>設定データ管理クラスのインスタンス</returns>
        public static ConfigDataManager LoadAppConfig(string cfgFileName, out string errorStr)
        {
            errorStr = "";

            // 設定パラメータ取得
            var prm = new ConfigDataParam();
            if (!LoadAppConfig(cfgFileName, prm, out errorStr))
            {
                return null;
            }

            return new ConfigDataManager(prm);
        }

        private ConfigDataParam m_configData;

        /// <summary>
        /// コンストラクタ。LoadAppConfig()からのみ生成される
        /// </summary>
        /// <param name="prm">設定データ</param>
        private ConfigDataManager(ConfigDataParam prm) : base(prm)
        {
            m_configData = prm;

            /// ユーザデータファイル名
            UserDataFileName = "UserDataAI.xml";
        }

        UserDataAI m_userData;

        /// <summary>
        /// 外観検査ユーザデータ読み出し
        /// </summary>
        /// <returns>成功/失敗(アプリ終了)</returns>
        public bool LoadUserData()
        {
            if (!LoadUserData(out m_userData))
            {
                bool mret = CommonUI.MsgBox.WarningConfirm("ユーザデータファイル読み込みでエラーがありました。\n新しいユーザデータファイルを作成しますか？");
                if (mret)
                {
                    m_userData = new UserDataAI();
                    if (SaveUserData())
                    {
                        return true;
                    }
                    CommonUI.MsgBox.Error("ユーザデータファイルを保存できません");
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 外観検査ユーザデータ書き込み
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool SaveUserData()
        {
            if (!SaveUserData(m_userData))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// ログ出力先
        /// </summary>
        public string LogDir
        {
            get { return m_configData.LogDir; }
        }

        /// <summary>
        /// ログ出力フィルター
        /// </summary>
        public string LogFilter
        {
            get { return m_configData.LogFilter; }
        }

        /// <summary>
        /// ログ自動削除日数
        /// </summary>
        public int LogAutoEraseDays
        {
            get { return m_configData.LogAutoEraseDays; }
        }

        /// <summary>
        /// 接続機器設定ファイルのリード<br/>接続機器ごとに存在する複数の設定ファイルを読み込み、設定値を保存する
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool LoadDeviceConfigFiles()
        {
            return LoadDeviceConfigFiles(Enum.GetValues(typeof(Device.DevType)));
        }

        /// <summary>
        /// 機器設定データの取得
        /// </summary>
        /// <param name="type">機器種別</param>
        /// <returns>機器設定データ</returns>
        public ExpandoObject GetDeviceConfig(Enum type)
        {
            return GetDeviceConfig(type.ToString());
        }

        /// <summary>
        /// 最後に送信したレシピ番号
        /// </summary>
        public string LastRecipeNo
        {
            get { return m_userData.RecipeNo; }
            set { m_userData.RecipeNo = value; }
        }
        /// <summary>
        /// 前回CIMに報告した設備状態
        /// </summary>
        public CIMManager.EquipmentStatusKind LastErrorKind
        {
            get { return (CIMManager.EquipmentStatusKind)m_userData.ErrorKind; }
            set
            {
                if (value != CIMManager.EquipmentStatusKind.ReturnNormal) NowError = value;
                m_userData.ErrorKind = (int)value;
            }
        }
        private CIMManager.EquipmentStatusKind m_now_error = CIMManager.EquipmentStatusKind.ReturnNormal;
        /// <summary>
        /// 今回の設備状態
        /// </summary>
        public CIMManager.EquipmentStatusKind NowError
        {
            get { return m_now_error; }
            private set { m_now_error = value; }
        }
        /// <summary>
        /// 未送信データリスト
        /// </summary>
        public List<CIMManager.Common.FTPServerQueData> UnsendDataList
        {
            get { return m_userData.UnsendDataList; }
            set { m_userData.UnsendDataList = value; }
        }

        /// <summary>
        /// 設定データクラス<br/>アプリケーション設定ファイルから抽出したデータ
        /// </summary>
        private class ConfigDataParam : ConfigBaseParam
        {
            /// <summary>
            /// ログ出力先フォルダ
            /// </summary>
            [May(@"Log")]
            public string LogDir { get; private set; }
            /// <summary>
            /// ログ出力レベル指定
            /// </summary>
            [May("")]
            public string LogFilter { get; private set; }
            /// <summary>
            /// ログ自動削除日数
            /// </summary>
            [May(30)]
            public int LogAutoEraseDays { get; private set; }
        }
    }

    /// <summary>
    /// 外観検査アプリユーザ保存データ
    /// </summary>
    public class UserDataAI
    {
        /// <summary>
        /// 前回CIMに報告したレシピ番号(0001～9999)
        /// </summary>
        public string RecipeNo { get; set; }
        /// <summary>
        /// 前回CIMに報告した設備状態
        /// </summary>
        public int ErrorKind { get; set; }

        /// <summary>
        /// 前回CIMに未送信のデータ
        /// </summary>
        public List<CIMManager.Common.FTPServerQueData> UnsendDataList { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public UserDataAI()
        {
            ErrorKind = 7;
            UnsendDataList = new List<CIMManager.Common.FTPServerQueData>();
        }
    }
}
