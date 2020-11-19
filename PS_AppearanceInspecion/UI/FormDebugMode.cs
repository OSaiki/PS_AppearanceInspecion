using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS_AppearanceInspecion.UI
{
    /// <summary>
    /// 点灯検査アプリ デバッグモード画面
    /// </summary>
    public partial class FormDebugMode : CommonUI.FormDebugModeBase
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="devTbl">機器管理情報テーブル</param>
        public FormDebugMode(DataTable devTbl) 
        {
            InitializeComponent();

            var pageDev = new CommonUI.PageDevices();
            pageDev.SetDataTable(devTbl);
            AddTab("機器一覧", pageDev);

            AddTab("CIM", new CommonUI.PageCIM());
            AddTab("プリンター", new CommonUI.PagePrinter());
        }
    }
}
