using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS_AppearanceInspecion
{

    /// <summary>
    /// 検査工程
    /// </summary>
    enum InspectionStep
    {
        /// <summary>
        /// 初期化中
        /// </summary>
        Intialize,
        /// <summary>
        /// ID取得
        /// </summary>
        GetId,
        /// <summary>
        /// パネルセット
        /// </summary>
        SetPanel,
        /// <summary>
        /// 裏面検査
        /// </summary>
        InspectionBack,
        /// <summary>
        /// パネル反転
        /// </summary>
        ReversePanel,
        /// <summary>
        /// 表面検査
        /// </summary>
        InspectionFront,
        /// <summary>
        /// 未設定
        /// </summary>
        None
    }
}
