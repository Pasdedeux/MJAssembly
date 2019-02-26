using System;
using System.Collections;
using System.Collections.Generic;
using LitFramework;
using LitFramework.LitTool;
using UnityEngine;
using UnityEngine.Assertions;

public partial class DataModel:Singleton<DataModel>
{
    #region 关卡数据

    //通关次数
    public int SuccTimes { get; set; }

    private int _currentLevel = -1;
    /// <summary>
    /// 当前应加载的关卡数
    /// </summary>
    public int CurrentLevel
    {
        get { return _currentLevel < 0 ? (_currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1)) : _currentLevel; }
        set { _currentLevel = value; PlayerPrefs.SetInt("CurrentLevel", _currentLevel); }
    }

    /// <summary>
    /// 总关卡数
    /// </summary>
    public int TotalLevels { get; set; }

    /// <summary>
    /// 已解锁关卡
    /// </summary>
    public int UnLockedMaxLevel
    {
        get { return PlayerPrefs.GetInt("Player_UnLockedMaxLevel", 1); }
        set { PlayerPrefs.SetInt("Player_UnLockedMaxLevel", value); }
    }

    private int _totalGold = -1;
    /// <summary>
    /// 当前拥有的金币
    /// </summary>
    public int TotalGold
    {
        get { return _totalGold < 0 ? (_totalGold = PlayerPrefs.GetInt("TotalGold", 0)) : _totalGold; }
        set { _totalGold = value; PlayerPrefs.SetInt("TotalGold", _totalGold); }
    }

    /// <summary>
    /// 获取当前关卡的记录通关时间
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public int GetLevelTime(int level)
    {
        return PlayerPrefs.GetInt(string.Format("LevelTime_{0}", level), -1);
    }


    /// <summary>
    /// 关卡星数
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public int GetLevelStarNum(int level)
    {
        return PlayerPrefs.GetInt( string.Format( "LevelStarNum_{0}", level, 0 ) );
    }

    /// <summary>
    /// 关卡星数
    /// </summary>
    /// <param name="level"></param>
    /// <param name="starNum"></param>
    public void SetLevelStarNum(int level , int starNum)
    {
        if ( GetLevelStarNum( level ) >= starNum ) return;
        PlayerPrefs.SetInt( string.Format( "LevelStarNum_{0}", level ), starNum );
    }
    #endregion

    #region 时间相关

    /// <summary>
    /// 获取时间戳Timestamp  
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public int GetTimeStamp(DateTime dt)
    {
        DateTime dateStart = new DateTime(1970, 1, 1, 8, 0, 0);
        int timeStamp = Convert.ToInt32((dt - dateStart).TotalSeconds);
        return timeStamp;
    }

    /// <summary>
    /// 时间戳Timestamp转换成日期
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    public DateTime GetDateTime(int timeStamp)
    {
        DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
        long lTime = ((long)timeStamp * 10000000);
        TimeSpan toNow = new TimeSpan(lTime);
        DateTime targetDt = dtStart.Add(toNow);
        return targetDt;
    }

    #endregion

    #region 游戏设置相关

    public bool UseNotify
    {
        get { return PlayerPrefs.GetInt( "Notify", 1 ) == 1; }
        set { PlayerPrefs.SetInt( "Notify", value ? 1 : 0 ); }
    }

    public bool UseZoom
    {
        get { return PlayerPrefs.GetInt( "Zoom", 1 ) == 1; }
        set { PlayerPrefs.SetInt( "Zoom", value ? 1 : 0 ); }
    }

    public bool GetUseVibrate()
    {
        return PlayerPrefs.GetInt("Vibrate", 1) == 1;
    }

    public void SetUseVibrate(bool use)
    {
        PlayerPrefs.SetInt("Vibrate", use ? 1 : 0 );
    }

    #endregion

    #region 关卡内状态
    public event Action DelLevelRestartCallBack;
    public event Action DelLevelOver;
    public event Action DelLevelClearCards;
    public event Action<int> DelLevelAddCard;
    public event Action DelStartGame;

    public void AddCard(int cardID)
    {
        if ( DelLevelAddCard != null ) DelLevelAddCard( cardID );
    }

    public void ClearCards()
    {
        if ( DelLevelClearCards != null ) DelLevelClearCards();
    }


    public void StartGame()
    {
        if ( DelStartGame != null ) DelStartGame();
    }

    /// <summary>
    /// 当前关卡星数
    /// </summary>
    public int CurrentStarNum { get; set; }

    public void LevelOver()
    {
        if ( DelLevelOver != null ) DelLevelOver();
    }

    public void Restart()
    {
        CurrentStarNum = 3;
        if ( DelLevelRestartCallBack !=null) DelLevelRestartCallBack();
    }

    public float ComboLeftTime { get; set; }
    public int ComboLevel { get; set; }

    private int _hintNum = -1, _shuffleNum=-1;
    public int HintNum
    {
        get { return _hintNum < 0 ? ( _hintNum = PlayerPrefs.GetInt( "HintNum", CardConfig.Instance.HintNum ) ) : _hintNum; }
        set { _hintNum = value; PlayerPrefs.SetInt( "HintNum", _hintNum ); }
    }

    public int ShuffleNum
    {
        get { return _shuffleNum < 0 ? ( _shuffleNum = PlayerPrefs.GetInt( "ShuffleNum", CardConfig.Instance.ShuffleNum ) ) : _shuffleNum; }
        set { _shuffleNum = value; PlayerPrefs.SetInt( "ShuffleNum", _shuffleNum ); }
    }



    #endregion

    #region Stuck
    public enum StuckType
    {
        GameStuck,
        Shuffle,
        Hint,
    }

    /// <summary>
    /// 当前弹窗类型
    /// </summary>
    public StuckType CurrentStuckType { get; set; }
    #endregion

    public ResultStatus ResultStatus { get; set; }

    /// <summary>
    /// 卡牌等待的基础位置右下最右点
    /// </summary>
    /// <value>The cards wait position.</value>
    public Vector3 CardsWaitPos { get; set; }

    /// <summary>
    /// 读取配置，目前是总关数
    /// </summary>
    public void LoadConfigs()
    {
        LitTool.monoBehaviour.StartCoroutine( ILoading() );
    }

    IEnumerator ILoading()
    {
        var filepath = AssetPathManager.Instance.GetStreamAssetDataPath( "levels.dat", true );
        yield return DocumentAccessor.Instance.WWWLoading( filepath, ( www ) =>
        {
            string levels = www.text;
            TotalLevels = levels.Split( '|' ).Length;
        }
        );
    }

    #region 商城
    public event Action DelUpdateShopStatus;
    public List<int> boughtIndex = new List<int>();

    public void UpdateShopStatus()
    {
        if ( DelUpdateShopStatus != null ) DelUpdateShopStatus();
    }

    /// <summary>
    /// 获取去广告价格
    /// </summary>
    /// <returns></returns>
    public string GetRemoveAdsPrice()
    {
        //todo 临时固定美元价格
        return "$ 2.99";
    }

    public void GetBuyList(ref List<int> buyArray)
    {
        //todo 固定返回已购买的物品
        buyArray.Clear();
        buyArray.AddRange( boughtIndex );
    }





    private List<string> _shopNames = new List<string>()
    { "Golden Eyes", "Super Bulb", "Magical Hand", "Small Pack", "BigPack" };
    private List<string> _shopDeses = new List<string>()
    { "Highlight free tiles", "Infinite Hints", "Infinite Hints", "", "" };
    private List<string> _shopPrice = new List<string>()
    { "$ 1.99", "$ 1.99", "$ 1.99", "$ 2.99", "$ 4.99" };
    /// <summary>
    /// 商店多个物品价格
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public string GetShopPrice(int index)
    {
        return LanguageModel.Instance.GetString( _shopPrice[ index ] );
    }

    public string GetShopName(int index)
    {
        return LanguageModel.Instance.GetString( _shopNames[ index ] );
    }

    public string GetShopDes(int index)
    {
        return LanguageModel.Instance.GetString( _shopDeses[ index ] );
    }

    #endregion


    public class UI
    {
        //UI
        public static string UI_RESULT = "Prefabs/UI/Canvas_Result";
        public static string UI_MAIN = "Prefabs/UI/Canvas_MainMenu";
        public static string UI_GAME = "Prefabs/UI/Canvas_Game";
        public static string UI_PAUSE = "Prefabs/UI/Canvas_Pause";
        public static string UI_SETTING = "Prefabs/UI/Canvas_Setting";
        public static string UI_STUCK = "Prefabs/UI/Canvas_Stuck";
        public static string UI_HELP = "Prefabs/UI/Canvas_Help";
        public static string UI_SHOP = "Prefabs/UI/Canvas_Shop";
        public static string UI_LEVELS = "Prefabs/UI/Canvas_Levels";
    }

    public class Sound
    {
        //Sound
        public static string Sound_LOGO = "Sound/logo";
        public static string Sound_SWITCHUI = "Sound/switchui";
        public static string Sound_CREATECARDS = "Sound/create";
        public static string Sound_CB_1 = "Sound/combo1";
        public static string Sound_CB_2 = "Sound/combo2";
        public static string Sound_CB_3 = "Sound/combo3";
        public static string Sound_CB_4 = "Sound/combo4";
        public static string Sound_CB_5 = "Sound/combo5";
        public static string Sound_UNDO = "Sound/undo";
        public static string Sound_HINT = "Sound/hint";
        public static string Sound_SHUFFLE = "Sound/shuffle";
        public static string Sound_GANG = "Sound/gang";
        public static string Sound_FAIL = "Sound/fail";
        public static string Sound_WIN = "Sound/win";
        public static string Sound_GOTSTAR = "Sound/gotstar";
        public static string Sound_OPENUI = "Sound/openui";
        public static string Sound_CLOSEUI = "Sound/closeui";
        public static string Sound_NOCHOOSE = "Sound/nochoose";
        public static string Sound_BGM = "Sound/bgm";
        public static string Sound_CLICK = "Sound/click";
        public static string Sound_ShopSucc = "Sound/pruchase";
    }

}

/// <summary>
/// 麻将当前状态
/// </summary>
public enum CardStatus
{
    /// <summary>
    /// 不可用
    /// </summary>
    UnUseable = 0,
    /// <summary>
    /// 可撤销
    /// </summary>
    Wait,
    /// <summary>
    /// 不可消除
    /// </summary>
    Fixed,
    /// <summary>
    /// 可消除
    /// </summary>
    CanUse,
    /// <summary>
    /// 已消除
    /// </summary>
    Used,
}

/// <summary>
/// 实例化物品类型
/// </summary>
public enum SpwanType
{
    BaseCards,
    Effect,
    Image,
}

/// <summary>
/// 每回合胜负
/// </summary>
public enum ResultStatus
{
    Continue,
    Success,
    Fail
}

/// <summary>
/// 基础麻将数据模型
/// </summary>
public class BaseCard
{
    #region 数据

    /// <summary>
    /// 麻将花色id
    /// </summary>
    public int ID = -1;
    /// <summary>
    /// 场景所占中心格子索引
    /// </summary>
    public int Index = -1;
    /// <summary>
    /// 当前所在行数
    /// </summary>
    public int RowIndex = -1;
    /// <summary>
    /// 当前所在列数
    /// </summary>
    public int ColIndex = -1;
    /// <summary>
    /// 层级
    /// </summary>
    public int Layer = -1;
    /// <summary>
    /// 麻将状态
    /// </summary>
    public CardStatus Status { get; set; }
    /// <summary>
    /// 左侧占据三个索引 (上->下)
    /// </summary>
    public int[] leftIndexs = new int[3] { -1, -1, -1 };
    /// <summary>
    /// 右侧占据三个索引 (上->下)
    /// </summary>
    public int[] rightIndexs = new int[3] { -1, -1, -1 };
    //Image
    public SpriteRenderer sprite;

    public BoxCollider boxColider;

    #endregion

    #region 模型
    public GameObject cardObj;
    #endregion

    public BaseCard(int id, int layer, int index)
    {
        ID = id;
        Layer = layer;
        Index = index;
        Status = CardStatus.UnUseable;
    }

    /// <summary>
    /// 更新 左右边界 点
    /// </summary>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public void UpdateBounds(int width, int height)
    {
        RowIndex = Mathf.FloorToInt((float)Index / width);
        ColIndex = Index % width;

        leftIndexs[0] = (RowIndex - 1) * width + ColIndex - 1;
        leftIndexs[1] = RowIndex * width + ColIndex - 1;
        leftIndexs[2] = (RowIndex + 1) * width + ColIndex - 1;

        rightIndexs[0] = (RowIndex - 1) * width + ColIndex + 1;
        rightIndexs[1] = RowIndex * width + ColIndex + 1;
        rightIndexs[2] = (RowIndex + 1) * width + ColIndex + 1;
    }

}


/// <summary>
/// 牌组事件
/// </summary>
public class CardsEvent
{
    public List<BaseCard> targetsCardsList;
    public List<int> originIndexCardsList;
    public List<int> originLayerList;

    public CardsEvent()
    {
        originIndexCardsList = new List<int>();
        targetsCardsList = new List<BaseCard>();
        originLayerList = new List<int>();
    }

    public void Dispose()
    {
        originIndexCardsList.Clear();
        targetsCardsList.Clear();
        originLayerList.Clear();
        originLayerList = null;
        targetsCardsList = null;
        originIndexCardsList = null;
    }

}

/// <summary>
/// 关卡数据结构
/// </summary>
public class LevelData
{
    public int LevelID;
    public int RowNum;
    public int ColNum;
    public int LayerNum;
    public int ColorNum;
    public int FlowerNum;
    public int CanPairNum;
    public double GridOffsetX;
    public double GridOffsetY;
    public int FullStarTime;

    //字典存储各层Index信息
    public List<LayerData> IndexListPerLayerList = new List<LayerData>();
}

public class LayerData
{
    public int LayerID;
    public List<int> LayerIndexList = new List<int>();
}
