using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Dynamic;
using CommonLib;

namespace Device
{
    /// <summary>
    /// 外観検査アプリデバイス管理
    /// </summary>
    class DeviceManager : DeviceManagerBase
    {
        /// <summary>
        /// CIM
        /// </summary>
        public DevCIM CIM { get; private set; }
        /// <summary>
        /// PLC
        /// </summary>
        public DevPLC PLC { get; private set; }
        /// <summary>
        /// プリンター
        /// </summary>
        public DevPrinter Printer { get; private set; }


        /// <summary>
        /// 機器のインスタンス生成
        /// </summary>
        /// <returns>成功/失敗</returns>
        public bool CreateDevices()
        {
            try
            {
                CIM = new DevCIM();
                PLC = new DevPLC();
                Printer = new DevPrinter();

                AddDevice(DevType.CIM, CIM);
                AddDevice(DevType.PLC, PLC);
                AddDevice(DevType.Printer, Printer);
            }
            catch (Exception e)
            {
                log.Error("インスタンス生成エラー:{0}", e);
                return false;
            }

            // データテーブル作成
            CreateTable();

            return true;
        }


        /// <summary>
        /// 機器設定値反映
        /// </summary>
        /// <param name="cfg">設定値クラス</param>
        /// <param name="err_dev">エラーがあった機器</param>
        /// <returns>成功/失敗</returns>
        public bool SetParamAll(Config.ConfigDataManager cfg, out Enum err_dev)
        {
            err_dev = default(Enum);
            foreach (var di in m_devices)
            {
                IDevice dev = di.Device;
                ExpandoObject eo = cfg.GetDeviceConfig(di.Type);
                if (eo == null)
                {
                    log.Error("必要な機器設定値が存在しません({0})", di.Type);
                    err_dev = di.Type;
                    return false;
                }
                if (!di.SetParam(eo))
                {
                    err_dev = di.Type;
                    return false;
                }
            }

            return true;
        }


    }
}
