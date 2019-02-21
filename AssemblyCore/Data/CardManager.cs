/*======================================
* 项目名称 ：Assets.Scripts.Data
* 项目描述 ：
* 类 名 称 ：CardManager
* 类 描 述 ：
* 命名空间 ：Assets.Scripts.Data
* 机器名称 ：DEREK-SURFACEPR 
* CLR 版本 ：4.0.30319.42000
* 作    者 ：Derek Liu
* 创建时间 ：2019/2/20 16:07:01
* 更新时间 ：2019/2/20 16:07:01
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ Inplayable 2019. All rights reserved.
*******************************************************************

-------------------------------------------------------------------
*Fix Note:
*修改时间：2019/2/20 16:07:01
*修改人： Derek Liu
*版本号： V1.0.0.0
*描述：
*
======================================*/

using Assets.Scripts;
using LitFramework;
using LitFramework.Input;
using LitFramework.LitTool;
using LitFramework.Mono;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

    public partial class CardManager
    {
        public static float CARDS_HALFWIDTH = 114 * 0.5f * 0.01f;
        public static float CARDS_HALFHEIGHT = 152 * 0.5f * 0.01f;
        public static float CARDS_XOFFSET = 16 * 0.01f;
        public static float CARDS_YOFFSET = 28 * 0.01f;

        private static int[] ALL_COLORS;
        private static int[] ALL_FLOWERS;
        private static int ALL_COLORS_NUM = 38;
        private static int ALL_FLOWERS_NUM = 8;

        /// <summary>
        /// 当前关卡各层矩阵
        /// </summary>
        private Dictionary<int, Dictionary<int, int>> _currentAllLayerDict;
        /// <summary>
        /// 当前保存的麻将牌信息
        /// </summary>
        private Dictionary<int, Dictionary<int, BaseCard>> _currentAllLevelsCards;
        /// <summary>
        /// 当前关卡麻将配置文件
        /// </summary>
        private Dictionary<int, Dictionary<int, BaseCard>> _currentAllCardsConfig;

        private Dictionary<int, int> _sortingOrderByIndex;

        //todo 这个应该是测试字典，编辑器部分用不到
        private List<int> _allCenterUseableDict;

        /// <summary>
        /// 麻将根结点
        /// </summary>
        private Transform _cardsRootTrans;
        /// <summary>
        /// 麻将数据仓库
        /// </summary>
        private Queue<BaseCard> _allBaseCardsQueue = new Queue<BaseCard>( 140 );

        //移动历史记录
        private Queue<CardsEvent> _cardMoveHistory = new Queue<CardsEvent>();

        //当前关卡格子点位数
        private int _rowPointsNum, _colPointsNum = 0;
        //当前关卡格子整体偏移值
        private float _offset_X, _offset_Y;
        //当前关卡格子层数
        private int _totalLayers;
        //消除记录显示区域
        private Vector3 _waitAndShowPos;
        //Effect: ShowChecked
        private Transform _checkedObj;
        //Effect:Hints
        private Transform _hintsObj1, _hintsObj2;
        
        //LevelData 
        private LevelData _levelData;
        //花色数量
        private int _colorNum;
        //花牌数量
        private int _flowerNum;
        //可配对最大数量
        private int _canPairNum;
        private int _currentTotalCardsNum;
        //三星通关时间
        private int _fullPassTime;

        private List<int> _currentColors;
        private List<int> _currentFlowers;
        //花牌/花色的   ID-数量 字典
        private Dictionary<int, int> _colorsRespDict;
        private Dictionary<int, int> _flowerRespDict;

        //可撤销标记
        private bool _canPlayBack = false;

        #region 场内数据模型

        /// <summary>
        /// 当前连击数量
        /// </summary>
        //private int _comboNum = 0;
        /// <summary>
        /// 剩余连击时间
        /// </summary>
        private float _comboLeftTime = 0f;


        #endregion


        //当前各层可用配对位置的中心索引点
        private Dictionary<int, List<int>> _curCanUseCardIndex;
        private CardConfig _cardConfig;


    #region 数据加载与处理

    /// <summary>
    /// 进入关卡的初始化启动
    /// </summary>
    public void LoadAllCards( int level )
    {
        //这里返回关卡配置字典
        _currentAllLayerDict.Clear();
        _allCenterUseableDict.Clear();


        //====================以下均为测试数据====================//
        ////todo 这里的配置要解析出每一个麻将的索引、层级、花色ID
        //_totalLayers = 4;
        ////todo 加载关卡格子尺寸
        //_rowPointsNum = 1 + 2 * 6;//3为牌组数量
        //_colPointsNum = 1 + 2 * 4;//3为牌组数量
        ////todo 地图格子偏移值
        //_offset_X = -1.41f;
        //_offset_Y = 0.19f;
        //_cardsRootTrans.position = new Vector3( _offset_X, _offset_Y, 0 );

        ////测试3层
        //for ( int i = 0; i < _totalLayers; i++ )
        //{
        //    //todo 生成动图格子点位数据
        //    GenerateMapGrid( i );

        //    List<BaseCard> levelCards = new List<BaseCard>();
        //    //todo 每层四张牌
        //    int cardsNumPerLayer = 10 - i;
        //    for ( int k = 0; k < cardsNumPerLayer; k++ )
        //        AddCards( AssignCard(), i, _allCenterUseableDict[ k ] );
        //}
        //====================================================//

        LitTool.monoBehaviour.StartCoroutine( ILoadLevelData( level ) );
    }

    private IEnumerator ILoadLevelData( int level )
    {
        var filepath = AssetPathManager.Instance.GetStreamAssetDataPath( string.Format( "level_{0}.dat", level ), true );

        yield return DocumentAccessor.Instance.WWWLoading
            ( filepath, ( w ) =>
            {
                _levelData = JsonMapper.ToObject<LevelData>( w.text );

                //解析出配置信息
                _totalLayers = _levelData.LayerNum;
                _rowPointsNum = _levelData.RowNum;
                _colPointsNum = _levelData.ColNum;
                _offset_X = ( float )_levelData.GridOffsetX;
                _offset_Y = ( float )_levelData.GridOffsetY;
                _cardsRootTrans.position = new Vector3( _offset_X, _offset_Y, 0 );
                _colorNum = _levelData.ColorNum;
                _flowerNum = _levelData.FlowerNum;
                _canPairNum = _levelData.CanPairNum;
                _fullPassTime = _levelData.FullStarTime;
                _currentTotalCardsNum = _levelData.IndexListPerLayerList.Sum( ( LayerData arg1 ) => { return arg1.LayerIndexList.Count; } );

                List<int> randomResult;
                //color,flower
                PrepairColorList_StepOne( out randomResult );
                //sorting
                SortingIndex();

                int indexTp = 0;
                for ( int k = 0; k < _totalLayers; k++ )
                {
                    GenerateMapGrid( k );

                    var layerData = _levelData.IndexListPerLayerList[ k ];
                    if ( layerData.LayerID != k ) Debug.LogError( "===config LayerID != k " + layerData.LayerID + "/" + k );

                    for ( int q = 0; q < layerData.LayerIndexList.Count; q++ )
                    {
                        int layerID = layerData.LayerID;
                        int index = layerData.LayerIndexList[ q ];
                        int id = randomResult[ indexTp ];//AssignCard( false );

                        AddCards( id, layerID, index );
                        indexTp++;
                    }
                }

                randomResult.Clear();
                randomResult = null;

                AudioManager.Instance.PlaySE( DataModel.Sound.Sound_CREATECARDS );

                DataModel.Instance.StartGame();
            }
        );

        yield return null;
        CameraController.Instance.ResetCamera();
        CheckCameraPos( true );

        yield return 0;
        Shuffle( true );
    }


    /// <summary>
    /// 根据配置/动态信息，生成对应花色组合，同时生成花牌组
    /// </summary>
    private void PrepairColorList_StepOne( out List<int> randomResult )
    {
        //选出花色
        int[] colorArrayOri = new int[ ALL_COLORS.Length ];
        ALL_COLORS.CopyTo( colorArrayOri, 0 );

        for ( int i = 0; i < _colorNum; i++ )
        {
            int result = UnityEngine.Random.Range( 0, colorArrayOri.Length - i );
            _currentColors.Add( colorArrayOri[ result ] );
            //_colorsRespDict.Add( colorArrayOri[ result ], 2 ); //选出来的牌成对出现

            int tmp = colorArrayOri[ colorArrayOri.Length - 1 - i ];
            colorArrayOri[ colorArrayOri.Length - 1 - i ] = colorArrayOri[ result ];
            colorArrayOri[ result ] = tmp;
        }

        //选出花牌
        int[] flowerArrayOri = new int[ ALL_FLOWERS.Length ];
        ALL_FLOWERS.CopyTo( flowerArrayOri, 0 );

        for ( int q = 0; q < _flowerNum; q++ )
        {
            int result = UnityEngine.Random.Range( 0, flowerArrayOri.Length - q );
            _currentFlowers.Add( flowerArrayOri[ result ] );
            //_flowerRespDict.Add( flowerArrayOri[ result ], 2 );//选出来的牌成对出现

            int tmp = flowerArrayOri[ flowerArrayOri.Length - 1 - q ];
            flowerArrayOri[ flowerArrayOri.Length - 1 - q ] = flowerArrayOri[ result ];
            flowerArrayOri[ result ] = tmp;
        }

        //为花色/花牌分配随机数量
        int total = _currentTotalCardsNum;
        int baseNum = _currentColors.Count * 2 + _currentFlowers.Count * 2;
        int multiple = ( int )Mathf.Floor( ( float )total / baseNum );
        int reassignNum = total - baseNum * multiple;

        //选出来的牌成对出现
        for ( int i = 0; i < _currentColors.Count; i++ )
            _colorsRespDict.Add( _currentColors[ i ], 2 * multiple ); //选出来的牌成对出现

        for ( int i = 0; i < _currentFlowers.Count; i++ )
            _flowerRespDict.Add( _currentFlowers[ i ], 2 * multiple ); //选出来的牌成对出现

        if ( reassignNum < 0 )
            throw new Exception( string.Format( "Colors/Flowers pairs are too many:{0}/{1}/{2}", _currentColors.Count, _currentFlowers.Count, total ) );

        List<int> totalList = new List<int>();
        totalList.AddRange( _currentColors );
        totalList.AddRange( _currentFlowers );

        //剩余分配
        while ( reassignNum > 0 )
        {
            int randomIndex = UnityEngine.Random.Range( 0, totalList.Count );
            int who = totalList[ randomIndex ];
            //花色区
            if ( randomIndex < _currentColors.Count )
                _colorsRespDict[ who ] += 2;
            //花牌区
            else
                _flowerRespDict[ who ] += 2;
            reassignNum -= 2;
        }

        //生成随机排列组
        randomResult = new List<int>( _currentTotalCardsNum );
        for ( int i = _currentTotalCardsNum - 1; i > -1; i-- )
        {
            //剩余分配
            int randomIndex = UnityEngine.Random.Range( 0, totalList.Count );
            int who = totalList[ randomIndex ];

            randomResult.Add( who );
            //花色区
            if ( who < ALL_COLORS_NUM )
            {
                _colorsRespDict[ who ]--;
                if ( _colorsRespDict[ who ] <= 0 )
                    totalList.RemoveAt( randomIndex );
            }
            //花牌区
            else
            {
                _flowerRespDict[ who ]--;
                if ( _flowerRespDict[ who ] <= 0 )
                    totalList.RemoveAt( randomIndex );
            }

        }
        totalList.Clear();
        totalList = null;

        //原因待查，出现了牌组未回收的情况，导致每关完成以后备用牌组不足
        int supply = 140 - _allBaseCardsQueue.Count;
        for ( int q = 0; q < supply; q++ )
            _allBaseCardsQueue.Enqueue( new BaseCard( -1, -1, -1 ) );

        //GC.Collect();
    }

    /// <summary>
    /// 根据索引点设置sortingorder
    /// </summary>
    private void SortingIndex()
    {
        //全局左上角 
        int index = 0;
        _sortingOrderByIndex.Add( index, 0 );
        for ( int i = 0; i < _rowPointsNum - 1; i++ )
        {
            for ( int q = 0; q < _colPointsNum - 1; q++ )
            {
                //右一点
                if ( !_sortingOrderByIndex.ContainsKey( index + 1 ) )
                    _sortingOrderByIndex.Add( index + 1, _sortingOrderByIndex[ index ] + 2 );
                else
                    _sortingOrderByIndex[ index + 1 ] = _sortingOrderByIndex[ index ] + 2;
                //下一点
                if ( !_sortingOrderByIndex.ContainsKey( q + ( i + 1 ) * _colPointsNum ) )
                    _sortingOrderByIndex.Add( q + ( i + 1 ) * _colPointsNum, _sortingOrderByIndex[ index ] + 1 );
                else
                    _sortingOrderByIndex[ q + ( i + 1 ) * _colPointsNum ] = _sortingOrderByIndex[ index ] + 1;

                index++;
            }
            index++;
        }
        //全局右下角
        _sortingOrderByIndex.Add( _rowPointsNum * _colPointsNum - 1, _sortingOrderByIndex[ _rowPointsNum * _colPointsNum - 2 ] + 2 );
    }

    /// <summary>
    /// 离开场景所有麻将销毁
    /// </summary>
    public void UnLoadAllCards()
    {
        _rowPointsNum = 0;
        _colPointsNum = 0;

        HideHintsEffect();

        foreach ( var item in _currentAllLevelsCards.Values )
        {
            var list = item.ToList();
            for ( int i = list.Count - 1; i > -1; i-- )
                RemoveCards( list[ i ].Value, true );
        }
        _currentAllCardsConfig.Clear();
        _currentAllLevelsCards.Clear();
        _sortingOrderByIndex.Clear();
        _currentFlowers.Clear();
        _currentColors.Clear();
        _colorsRespDict.Clear();
        _flowerRespDict.Clear();
        _lastTouchedCards = null;

        ClearHistory();

    }


    public LevelData GetCurrentLevelData()
    {
        return _levelData;
    }


    private void GenerateMapGrid( int layer )
    {
        int index = 0;
        for ( int i = 0; i < _rowPointsNum; i++ )
        {
            for ( int k = 0; k < _colPointsNum; k++ )
            {
                if ( i % 2 == 1 && k % 2 == 1 ) _allCenterUseableDict.Add( index );
                AddOccupyIndex( index, layer );
                index++;
            }
        }
    }

    /// <summary>
    /// 向网盘打点
    /// </summary>
    /// <param name="index">Index.</param>
    private void AddOccupyIndex( int index, int layerIndex )
    {
        if ( !_currentAllLayerDict.ContainsKey( layerIndex ) )
            _currentAllLayerDict.Add( layerIndex, new Dictionary<int, int>() );

        if ( !_currentAllLayerDict[ layerIndex ].ContainsKey( index ) )
            _currentAllLayerDict[ layerIndex ].Add( index, 0 );
        else
            _currentAllLayerDict[ layerIndex ][ index ]++;
    }

    /// <summary>
    /// 减少网盘点位打点次数1
    /// </summary>
    /// <param name="index">Index.</param>
    /// <param name="layerIndex">Layer index.</param>
    private void RemoveOccupyIndex( int index, int layerIndex )
    {
        //移动到边界处于可撤销状态时，设边界索引值为-1，同时视为标记
        if ( index < 0 ) return;
        try
        {
            _currentAllLayerDict[ layerIndex ][ index ] = Mathf.Max( 0, _currentAllLayerDict[ layerIndex ][ index ] - 1 );
        }
        catch ( Exception ex )
        {
            throw new Exception( string.Format( "Point Index Error: layerIndex>{0}:index>{1}-{2} ", layerIndex, index, ex ) );
        }
    }


    #endregion

    #region 牌组的创造与销毁

    /// <summary>
    /// 分配牌型花色
    /// </summary>
    /// <returns></returns>
    private int AssignCard( bool needPair )
    {
        //需要一组可配对牌组
        if ( needPair )
        {

        }
        //随机任意取值
        else
        {

        }
        //todo 先暂时随机发牌
        return UnityEngine.Random.Range( 0, 42 );
    }

    /// <summary>
    /// 增加一个麻将
    /// </summary>
    /// <returns>The cards.</returns>
    /// <param name="id">Identifier.</param>
    /// <param name="layer">Layer.</param>
    /// <param name="index">Index.</param>
    private BaseCard AddCards( int id, int layer, int index )
    {
        BaseCard cards = _allBaseCardsQueue.Dequeue();
        if ( cards == null ) cards = new BaseCard( -1, -1, -1 );

        //init all params
        cards.ID = id;
        cards.Layer = layer;
        cards.Index = index;
        cards.UpdateBounds( _colPointsNum, _rowPointsNum );

        CreateCardObj( cards );

        //update bound index recording
        for ( int w = 0; w < cards.leftIndexs.Length; w++ )
        {
            AddOccupyIndex( cards.leftIndexs[ w ], cards.Layer );
            AddOccupyIndex( cards.rightIndexs[ w ], cards.Layer );
        }
        AddOccupyIndex( cards.Index, cards.Layer );
        AddOccupyIndex( cards.leftIndexs[ 0 ] + 1, cards.Layer );
        AddOccupyIndex( cards.leftIndexs[ 2 ] + 1, cards.Layer );

        AddToDict( layer, cards, true );
        return cards;
    }


    /// <summary>
    /// 从场上移除麻将
    /// </summary>
    private void RemoveCards( BaseCard baseCards, bool all = false )
    {
        RemoveCartdsOnlyObj( baseCards );
        RemoveCardsOnlyData( baseCards,all );
    }


    private void RemoveCardsOnlyData( BaseCard baseCards , bool all = false )
    {
        //update bound index recording
        for ( int w = 0; w < baseCards.leftIndexs.Length; w++ )
        {
            RemoveOccupyIndex( baseCards.leftIndexs[ w ], baseCards.Layer );
            RemoveOccupyIndex( baseCards.rightIndexs[ w ], baseCards.Layer );
        }
        RemoveOccupyIndex( baseCards.Index, baseCards.Layer );
        RemoveOccupyIndex( baseCards.leftIndexs[ 0 ] + 1, baseCards.Layer );
        RemoveOccupyIndex( baseCards.leftIndexs[ 2 ] + 1, baseCards.Layer );

        RemoveFromDict( baseCards.Layer, baseCards, all );

        baseCards.Status = CardStatus.UnUseable;
        _allBaseCardsQueue.Enqueue( baseCards );
    }


    private void RemoveCartdsOnlyObj(BaseCard baseCard)
    {
        DestroyCardsObj( baseCard );
    }



    /// <summary>
    /// 逻辑：场上移动麻将到指定层位置
    /// </summary>
    /// <param name="index">Index.-1视为可撤销位置</param>
    /// <param name="layer">Layer.-1视为可撤销位置</param>
    private void MoveCardsLogicalTo( BaseCard baseCards, int index, int layer, bool needRemoveDict = true )
    {
        //目前只记录到可撤销位置时的移动记录
        if ( index < 0 || layer < 0 )
        {
            CardsEvent cardsEvent = new CardsEvent();
            cardsEvent.targetsCardsList.Add( baseCards );
            cardsEvent.originLayerList.Add( baseCards.Layer );
            cardsEvent.originIndexCardsList.Add( baseCards.Index );
            _cardMoveHistory.Enqueue( cardsEvent );
            Debug.Log( "记录撤销位置" + _cardMoveHistory.Count );
        }

        //当洗牌时，不需要移动前清楚原位置记录，因可能清掉新的已经移动到原位置上的牌组
        //update bound index recording
        for ( int w = 0; w < baseCards.leftIndexs.Length; w++ )
        {
            RemoveOccupyIndex( baseCards.leftIndexs[ w ], baseCards.Layer );
            RemoveOccupyIndex( baseCards.rightIndexs[ w ], baseCards.Layer );
        }
        RemoveOccupyIndex( baseCards.Index, baseCards.Layer );
        RemoveOccupyIndex( baseCards.leftIndexs[ 0 ] + 1, baseCards.Layer );
        RemoveOccupyIndex( baseCards.leftIndexs[ 2 ] + 1, baseCards.Layer );

        if ( needRemoveDict )
            RemoveFromDict( baseCards.Layer, baseCards, false );

        //定义：大于0的位置为棋盘位置，否则视为可撤销位置
        if ( index > -1 )
        {
            //init all params
            baseCards.Layer = layer;
            baseCards.Index = index;
            baseCards.UpdateBounds( _colPointsNum, _rowPointsNum );
            baseCards.cardObj.transform.name = string.Format( "{0}_{1}", baseCards.Layer.ToString(), baseCards.Index.ToString() );

            //update bound index recording
            for ( int w = 0; w < baseCards.leftIndexs.Length; w++ )
            {
                AddOccupyIndex( baseCards.leftIndexs[ w ], baseCards.Layer );
                AddOccupyIndex( baseCards.rightIndexs[ w ], baseCards.Layer );
            }
            AddOccupyIndex( baseCards.Index, baseCards.Layer );
            AddOccupyIndex( baseCards.leftIndexs[ 0 ] + 1, baseCards.Layer );
            AddOccupyIndex( baseCards.leftIndexs[ 2 ] + 1, baseCards.Layer );

            AddToDict( layer, baseCards );
        }
    }

    /// <summary>
    /// 回退步骤，指定回退步数
    /// </summary>
    /// <param name="stepNum">Step number.</param>
    public void PlayBackStep( int stepNum = 2 )
    {
        if ( !_canPlayBack ) return;

        AudioManager.Instance.PlaySE( DataModel.Sound.Sound_UNDO );

        while ( stepNum > 0 )
        {
            if ( _cardMoveHistory.Count > 0 )
            {
                CardsEvent cardsEvent = _cardMoveHistory.Dequeue();
                for ( int i = 0; i < cardsEvent.originIndexCardsList.Count; i++ )
                {
                    BaseCard baseCard = cardsEvent.targetsCardsList[ i ];
                    int oriIndex = cardsEvent.originIndexCardsList[ i ];
                    int oriLayer = cardsEvent.originLayerList[ i ];
                    baseCard.boxColider.enabled = true;

                    MoveCardsLogicalTo( baseCard, oriIndex, oriLayer );
                    EffectMoveTo( baseCard );
                }
            }
            stepNum--;
        }
        EndCombo();
        DataModel.Instance.ClearCards();
        //UpdateUseableStatus( true );
    }


    /// <summary>
    /// 执行配对，在已保存有点击牌型的情况下
    /// </summary>
    /// <param name="curBaseCard">Base card.</param>
    private void PickPair( BaseCard curBaseCard )
    {
        if ( CanPickPair( curBaseCard ) )
        {
            curBaseCard.Status = CardStatus.Used;
            _lastTouchedCards.Status = CardStatus.Used;

            Checked( false, 20000, Vector3.zero );

            curBaseCard.boxColider.enabled = false;
            _lastTouchedCards.boxColider.enabled = false;

            //杠
            if ( _cardMoveHistory.Count > 0 && _cardMoveHistory.Count % 2 == 0 )
            {
                var pickOne = _cardMoveHistory.Dequeue();
                //任意一个比较
                if ( pickOne.targetsCardsList[ 0 ].ID < 38 && pickOne.targetsCardsList[ 0 ].ID == _lastTouchedCards.ID )
                {
                    StartGang();
                    _cardMoveHistory.Enqueue( pickOne );
                }
                else
                {
                    _cardMoveHistory.Enqueue( pickOne );

                    DataModel.Instance.ClearCards();
                    ClearHistory();
                }
            }

            //用于处理杠之后，历史记录清除，但是展示牌组尚未清除时，取值错误
            if ( _cardMoveHistory.Count == 0 )
                DataModel.Instance.ClearCards();

            //配对成功，逻辑消除棋盘记
            MoveCardsLogicalTo( curBaseCard, -1, -1 );
            MoveCardsLogicalTo( _lastTouchedCards, -1, -1 );

            DataModel.Instance.AddCard( curBaseCard.ID );
            DataModel.Instance.AddCard( _lastTouchedCards.ID );

            //移动动画，右下角待定
            EffectMoveToMiddlePoint( curBaseCard, _lastTouchedCards );

            //相机位置检测
            //CheckCameraPos();

            //刷新场上牌组可连性状态更新
            StartCombo();
            UpdateUseableStatus();

            CheckVictory();
            if ( DataModel.Instance.ResultStatus != ResultStatus.Continue )
            {
                StopEffect();
                DataModel.Instance.LevelOver();
                DataModel.Instance.SetLevelStarNum( DataModel.Instance.CurrentLevel, DataModel.Instance.CurrentStarNum );
                UIManager.Instance.Show( DataModel.UI.UI_RESULT );
                return;
            }
        }
        else
        {
            //配对失败，震动，消除记录
            EffectMatchFail( curBaseCard );
            //关闭combo效果
            //EndCombo();

            if ( curBaseCard.ID != _lastTouchedCards.ID )
            {
                _lastTouchedCards = curBaseCard;
                Checked( true, AssignSortingOrder( _lastTouchedCards ) + 1, _lastTouchedCards.cardObj.transform.position );
                return;
            }
        }
        Checked( false, 20000, Vector3.zero );
        _lastTouchedCards = null;
    }

    Vector3[] _nowBound = new Vector3[ 4 ];
    Vector3[] _camBound = new Vector3[ 4 ];
    /// <summary>
    /// 相机位置修正
    /// </summary>
    private void CheckCameraPos( bool force = false )
    {
        Vector3[] bounds = CameraController.Instance.GetEffectBounds();
        _nowBound[ 0 ] = _nowBound[ 1 ] = _nowBound[ 2 ] = _nowBound[ 3 ] = Vector3.zero;

        foreach ( var item in _currentAllLevelsCards.Values )
        {
            foreach ( var basecards in item.Values )
            {
                Vector3 pos = basecards.cardObj.transform.position;
                //左右
                if ( _nowBound[ 0 ].x > pos.x )
                    _nowBound[ 0 ] = pos;
                else if ( _nowBound[ 1 ].x < pos.x )
                    _nowBound[ 1 ] = pos;
                //上下
                if ( _nowBound[ 2 ].y < pos.y )
                    _nowBound[ 2 ] = pos;
                else if ( _nowBound[ 3 ].y > pos.y )
                    _nowBound[ 3 ] = pos;
            }
        }
        float nowWidth = Mathf.Abs( _nowBound[ 1 ].x - _nowBound[ 0 ].x );
        float camWidth = Mathf.Abs( bounds[ 1 ].x - bounds[ 0 ].x );

        float nowHeight = Mathf.Abs( _nowBound[ 3 ].y - _nowBound[ 2 ].y );
        float camHeight = Mathf.Abs( bounds[ 2 ].y - bounds[ 1 ].y );

        float widthRatio = nowWidth / camWidth;
        float heightRatio = nowHeight / camHeight;

        if ( !force && ( widthRatio >= 0.7f || heightRatio >= 0.7f ) )
        {
            CameraController.Instance.MoveCamera( new Vector3( ( _nowBound[ 1 ].x + _nowBound[ 0 ].x ) * .5f, ( _nowBound[ 3 ].y + _nowBound[ 2 ].y ) * .5f, -100 ) );
            return;
        }
        CameraController.Instance.MoveCamera( new Vector3( ( _nowBound[ 1 ].x + _nowBound[ 0 ].x ) * .5f, ( _nowBound[ 3 ].y + _nowBound[ 2 ].y ) * .7f, -100 ), Mathf.Max( widthRatio, heightRatio ) );
    }

    /// <summary>
    /// 单局胜负结算
    /// </summary>
    /// <returns></returns>
    public void CheckVictory()
    {
        //todo 先暂时统计，低效率
        int remain = _currentAllLevelsCards.Sum( e => { return e.Value.Count; } );
        //通过剩余牌数判断是否是否弹出成功界面
        if ( remain > 0 )
        {
            //需要根据所生的牌中是否存在可配对数，判断是否弹窗失败界面
            if ( !CheckCardsCanContinue() )
            {
                DataModel.Instance.CurrentStuckType = DataModel.StuckType.GameStuck;
                DataModel.Instance.LevelOver();
                UIManager.Instance.Show( DataModel.UI.UI_STUCK );
            }
            DataModel.Instance.ResultStatus = ResultStatus.Continue;
        }
        else
        {
            DataModel.Instance.ResultStatus = ResultStatus.Success;
        }
    }

    /// <summary>
    /// 牌组是否可以继续
    /// </summary>
    /// <returns></returns>
    private bool CheckCardsCanContinue()
    {
        Dictionary<int, List<BaseCard>> canUsePairCards = new Dictionary<int, List<BaseCard>>();

        foreach ( var layerIndex in _curCanUseCardIndex )
        {
            for ( int i = 0; i < layerIndex.Value.Count; i++ )
            {
                BaseCard card = _currentAllLevelsCards[ layerIndex.Key ][ layerIndex.Value[ i ] ];
                int id = card.ID;
                if ( !canUsePairCards.ContainsKey( id ) )
                    canUsePairCards.Add( id, new List<BaseCard>() );
                canUsePairCards[ id ].Add( card );

                if ( canUsePairCards[ id ].Count > 1 ) return true;
            }
        }
        
        return false;
    }

    private bool CanPickPair( BaseCard curBaseCard )
    {
        //花色必须相同，或者同为花牌
        return curBaseCard.ID == _lastTouchedCards.ID
            || ( curBaseCard.ID > 37 && _lastTouchedCards.ID > 37 && curBaseCard.ID < 42 && _lastTouchedCards.ID < 42 )
            || ( curBaseCard.ID > 41 && _lastTouchedCards.ID > 41 );
    }


    /// <summary>
    /// 更新场上牌组可用性信息
    /// </summary>
    public void UpdateUseableStatus( bool terminate = false )
    {
        _curCanUseCardIndex.Clear();
        foreach ( var item in _currentAllLevelsCards.Values )
        {
            foreach ( var baseCardsDict in item )
            {
                if ( CenterIndexCanUse( baseCardsDict.Key, baseCardsDict.Value.Layer ) )
                {
                    baseCardsDict.Value.Status = CardStatus.CanUse;

                    if ( _curCanUseCardIndex.ContainsKey( baseCardsDict.Value.Layer ) )
                        _curCanUseCardIndex[ baseCardsDict.Value.Layer ].Add( baseCardsDict.Key );
                    else
                        _curCanUseCardIndex.Add( baseCardsDict.Value.Layer, new List<int>() { baseCardsDict.Key } );
                }
                else
                    baseCardsDict.Value.Status = CardStatus.Fixed;

                EffectUpdateCardsStatus( baseCardsDict.Value, terminate );
            }
        }
    }


    /// <summary>
    /// Clears the history of Removement.
    /// </summary>
    private void ClearHistory()
    {
        while ( _cardMoveHistory.Count > 0 )
        {
            var baseCardsEvent = _cardMoveHistory.Dequeue();
            //var baseCardsList = baseCardsEvent.targetsCardsList;
            //for ( int i = 0; i < baseCardsList.Count; i++ )
            //    RemoveCards( baseCardsList[ i ] );
        }
    }


    /// <summary>
    /// Creates the card by type, levels, and where to place it
    /// </summary>
    private void CreateCardObj( BaseCard baseCards )
    {
        if ( baseCards.cardObj != null ) DestroyCardsObj( baseCards );
        baseCards.cardObj = SpawnManager.Instance.SpawnObject( SpwanType.BaseCards, -1 );
        baseCards.cardObj.transform.SetParent( _cardsRootTrans );
        baseCards.cardObj.transform.name = string.Format( "{0}_{1}", baseCards.Layer.ToString(), baseCards.Index.ToString() );

        //Layer offset
        Vector3 pos = new Vector3(
            CARDS_HALFWIDTH * baseCards.ColIndex - baseCards.Layer * CARDS_XOFFSET,
            CARDS_HALFHEIGHT * baseCards.RowIndex * -1f + baseCards.Layer * CARDS_YOFFSET,
            -0.05f * baseCards.Index - 1 * baseCards.Layer );

        baseCards.cardObj.transform.localPosition = pos;
        baseCards.sprite = baseCards.cardObj.GetComponent<SpriteRenderer>();
        baseCards.boxColider = baseCards.cardObj.GetComponent<BoxCollider>();
        baseCards.sprite.sprite = Resources.Load<Sprite>( baseCards.ID.ToString() );
        baseCards.sprite.sortingOrder = AssignSortingOrder( baseCards );
    }

    /// <summary>
    /// Assigns an order number for sorting.
    /// </summary>
    /// <param name="baseCards">Base card.</param>
    private int AssignSortingOrder( BaseCard baseCards )
    {
        return ( baseCards.Layer * 1000 ) + _sortingOrderByIndex[ baseCards.Index ];
    }

    /// <summary>
    /// Destroies the cards and recycle the BaseCards.
    /// </summary>
    /// <param name="baseCards">Base cards.</param>
    private void DestroyCardsObj( BaseCard baseCards )
    {
        if ( baseCards.cardObj != null )
        {
            SpawnManager.Instance.DespawnObject( baseCards.cardObj );
            baseCards.cardObj = null;
        }
    }

    /// <summary>
    /// Adds to current dict/ config dict.
    /// </summary>
    /// <param name="layer">Layer.</param>
    /// <param name="baseCards">Base cards.</param>
    private void AddToDict( int layer, BaseCard baseCards, bool all = false )
    {
        if ( all )
        {
            if ( !_currentAllCardsConfig.ContainsKey( layer ) )
                _currentAllCardsConfig.Add( layer, new Dictionary<int, BaseCard>() );
            _currentAllCardsConfig[ layer ].Add( baseCards.Index, baseCards );
        }

        if ( !_currentAllLevelsCards.ContainsKey( layer ) )
            _currentAllLevelsCards.Add( layer, new Dictionary<int, BaseCard>() );
        _currentAllLevelsCards[ layer ].Add( baseCards.Index, baseCards );
    }

    /// <summary>
    /// Removes from all dicts.
    /// </summary>
    /// <param name="layer">Layer.</param>
    /// <param name="baseCards">Base cards.</param>
    private void RemoveFromDict( int layer, BaseCard baseCards, bool all = false )
    {
        if ( _currentAllLevelsCards.ContainsKey( layer ) )
            _currentAllLevelsCards[ layer ].Remove( baseCards.Index );
        if ( all ) _currentAllCardsConfig[ layer ].Remove( baseCards.Index );
    }
    #endregion

    #region 棋盘格状态
    public bool CenterIndexCanUse( int index, int layer )
    {
        //左右两侧无遮挡，上方无叠加
        if ( ( _currentAllLayerDict[ layer ][ index - 1 ] <= 1 || _currentAllLayerDict[ layer ][ index + 1 ] <= 1 )
        && ( !_currentAllLayerDict.ContainsKey( layer + 1 ) || _currentAllLayerDict[ layer + 1 ][ index ] == 0 ) )
        {
            return true;
        }
        return false;
    }
    #endregion


    #region Core

    #region 组合效果

    private void Checked( bool check, int index, Vector3 pos )
    {
        _checkedObj.position = check ? pos : Vector3.one * 999;
        _checkedObj.GetComponent<SpriteRenderer>().sortingOrder = check ? index + 1 : 20000;
    }


    #region Combo

    public event Action<float, int> DelCallComboInfo;

    /// <summary>
    /// 开启combo效果
    /// </summary>
    public void StartCombo()
    {
        _comboLeftTime = _cardConfig.ComboTime;
        if ( DataModel.Instance.ComboLevel < 5 )
            DataModel.Instance.ComboLevel++;

        AudioManager.Instance.PlaySE( string.Format( "Sound/combo{0}", DataModel.Instance.ComboLevel ) );

        if ( DelCallComboInfo != null ) DelCallComboInfo( _comboLeftTime, DataModel.Instance.ComboLevel );
    }

    /// <summary>
    /// 关闭combo效果
    /// </summary>
    public void EndCombo()
    {
        _comboLeftTime = -1;

        DataModel.Instance.ComboLevel = 0;
        if ( DelCallComboInfo != null ) DelCallComboInfo( _comboLeftTime, DataModel.Instance.ComboLevel );

        UpdateUseableStatus( true );
    }

    #endregion


    #region Gang

    private WaitForSeconds _gangWaitCoroutTime;
    /// <summary>
    /// 出发杠效果
    /// </summary>
    public void StartGang()
    {
        Dictionary<int, Stack<BaseCard>> canGangPairs = new Dictionary<int, Stack<BaseCard>>();
        foreach ( var item in _currentAllLevelsCards.Values )
        {
            var list = item.ToList();
            for ( int i = list.Count - 1; i > -1; i-- )
            {
                var baseCards = list[ i ].Value;
                if ( baseCards.Status == CardStatus.CanUse )
                {
                    if ( !canGangPairs.ContainsKey( baseCards.ID ) )
                        canGangPairs.Add( baseCards.ID, new Stack<BaseCard>() );
                    canGangPairs[ baseCards.ID ].Push( baseCards );
                }
            }
        }

        AudioManager.Instance.PlaySE( DataModel.Sound.Sound_GANG );

        LitTool.monoBehaviour.StartCoroutine( IGangEluminate( canGangPairs ) );
    }

    IEnumerator IGangEluminate( Dictionary<int, Stack<BaseCard>> canGangPairs )
    {
        InputControlManager.Instance.IsEnable = false;

        yield return null;

        foreach ( var item in canGangPairs.Values )
        {
            while ( item.Count > 1 )
            {
                var baseCards1 = item.Pop();
                var baseCards2 = item.Pop();

                EffectMoveToMiddlePoint( baseCards1, baseCards2, () =>
                {
                    RemoveCartdsOnlyObj( baseCards1 );
                    RemoveCartdsOnlyObj( baseCards2 );
                } );

                RemoveCardsOnlyData( baseCards1 );
                RemoveCardsOnlyData( baseCards2 );

                StartCombo();

                yield return _gangWaitCoroutTime;
            }
        }
        ClearHistory();
        UpdateUseableStatus();

        CheckCameraPos();

        InputControlManager.Instance.IsEnable = true;

        canGangPairs.Clear();
        canGangPairs = null;

        CheckVictory();
        if ( DataModel.Instance.ResultStatus != ResultStatus.Continue )
        {
            StopEffect();
            DataModel.Instance.LevelOver();
            DataModel.Instance.SetLevelStarNum( DataModel.Instance.CurrentLevel, DataModel.Instance.CurrentStarNum );
            UIManager.Instance.Show( DataModel.UI.UI_RESULT );
            yield break;
        }
    }

    #endregion

    #region 洗牌
    List<BaseCard> _shuffuleAllCards;
    List<int> _shuffleAllIndex;
    List<int> _shuffleAllLayer;

    /// <summary>
    /// 洗牌
    /// </summary>
    /// <param name="auto">If set manually</param>
    public void Shuffle( bool auto = false )
    {
        Checked( false, 20000, Vector3.zero );

        AudioManager.Instance.PlaySE( DataModel.Sound.Sound_SHUFFLE );

        //Step 1:登记现有所有牌组信息
        //equal to UpdateUseableStatus(true);
        _curCanUseCardIndex.Clear();
        _shuffuleAllCards.Clear();
        _shuffleAllIndex.Clear();
        _shuffleAllLayer.Clear();
        foreach ( var item in _currentAllLevelsCards.Values )
        {
            foreach ( var baseCardsDict in item )
            {
                if ( CenterIndexCanUse( baseCardsDict.Key, baseCardsDict.Value.Layer ) )
                {
                    baseCardsDict.Value.Status = CardStatus.CanUse;

                    if ( _curCanUseCardIndex.ContainsKey( baseCardsDict.Value.Layer ) )
                        _curCanUseCardIndex[ baseCardsDict.Value.Layer ].Add( baseCardsDict.Key );
                    else
                        _curCanUseCardIndex.Add( baseCardsDict.Value.Layer, new List<int>() { baseCardsDict.Key } );
                }
                else
                    baseCardsDict.Value.Status = CardStatus.Fixed;

                EffectUpdateCardsStatus( baseCardsDict.Value, true );

                //temple for restore the card info
                _shuffuleAllCards.Add( baseCardsDict.Value );
                _shuffleAllLayer.Add( baseCardsDict.Value.Layer );
                _shuffleAllIndex.Add( baseCardsDict.Value.Index );
            }
        }

        //Step 2:清除所有网格信息
        for ( int i = 0; i < _shuffuleAllCards.Count; i++ )
            RemoveFromDict( _shuffuleAllCards[ i ].Layer, _shuffuleAllCards[ i ] );

        //Step 3:优先配对放置，取最小可放置数目
        //赶时间，笨办法
        List<int> layerCanUseList = new List<int>();
        List<int> indexCanUseList = new List<int>();
        foreach ( var item in _curCanUseCardIndex )
        {
            foreach ( var index in item.Value )
            {
                indexCanUseList.Add( index );
                layerCanUseList.Add( item.Key );
            }
        }

        //Step 33:对于只剩下一个柱状排布时特殊处理，linq少用
        var resultList = indexCanUseList.GroupBy( e => e );
        if ( resultList.Count() > 1 )
        {
            //maintain can-use index list
            Debug.Log( _curCanUseCardIndex.Count );

            //指定配对组合
            int canPairNum = _canPairNum;
            int canUsePairNum = Mathf.FloorToInt( _curCanUseCardIndex.Sum( ( arg1 ) => { return arg1.Value.Count; } ) * .5f );
            canPairNum = Mathf.Min( canUsePairNum, canPairNum );

            for ( int q = 0; q < canPairNum; q++ )
            {
                int randomIndex = UnityEngine.Random.Range( 0, _shuffuleAllCards.Count );
                int randomID = _shuffuleAllCards[ randomIndex ].ID;
                List<BaseCard> sameIDCards;
                try
                {
                    //牌均取两张，前面的条件已保证此处可以正常获取不会越界
                    sameIDCards = _shuffuleAllCards.Where( e => { return e.ID == randomID; } ).ToList();
                    var card0 = sameIDCards[ 0 ];
                    var card1 = sameIDCards[ 1 ];
                    var layer0 = layerCanUseList[ layerCanUseList.Count - 1 ];
                    var layer1 = layerCanUseList[ layerCanUseList.Count - 2 ];
                    var useIndex0 = indexCanUseList[ indexCanUseList.Count - 1 ];
                    var useIndex1 = indexCanUseList[ indexCanUseList.Count - 2 ];

                    MoveCardsLogicalTo( card0, useIndex0, layer0, false );
                    EffectMoveTo( card0, !auto );

                    MoveCardsLogicalTo( card1, useIndex1, layer1, false );
                    EffectMoveTo( card1, !auto );

                    layerCanUseList.RemoveAt( layerCanUseList.Count - 1 );
                    layerCanUseList.RemoveAt( layerCanUseList.Count - 1 );
                    indexCanUseList.RemoveAt( indexCanUseList.Count - 1 );
                    indexCanUseList.RemoveAt( indexCanUseList.Count - 1 );
                    //删除备选项
                    _shuffuleAllCards.Remove( card0 ); _shuffuleAllCards.Remove( card1 );
                    _shuffleAllLayer.Remove( layer0 ); _shuffleAllLayer.Remove( layer1 );
                    _shuffleAllIndex.RemoveAt( _shuffleAllIndex.LastIndexOf( useIndex0 ) ); _shuffleAllIndex.RemoveAt( _shuffleAllIndex.LastIndexOf( useIndex1 ) );
                }
                catch ( Exception ex )
                {
                    Debug.Log( ":" + ex.Message );
                }

            }

            //Step 4：剩下的随机放置
            for ( int i = 0; i < _shuffuleAllCards.Count; i++ )
            {
                int randomindex = UnityEngine.Random.Range( 0, _shuffleAllIndex.Count );
                BaseCard randomTarget = _shuffuleAllCards[ i ];

                Debug.Log( "index: " + randomTarget.Index + " => " + _shuffleAllIndex[ randomindex ] );
                Debug.Log( "layer: " + randomTarget.Layer + " => " + _shuffleAllLayer[ randomindex ] );

                MoveCardsLogicalTo( randomTarget, _shuffleAllIndex[ randomindex ], _shuffleAllLayer[ randomindex ], false );
                EffectMoveTo( randomTarget, !auto );

                _shuffleAllIndex.RemoveAt( randomindex );
                _shuffleAllLayer.RemoveAt( randomindex );
            }
        }
        else
        {
            //全部放到最底层2个一组并排排列
            int rowIndex = -1;
            int centerIndex = 0;
            int baseCenterIndex = ( int )( _colPointsNum - 1 ) / 2;
            for ( int q = 0; q < _shuffuleAllCards.Count; q++ )
            {
                if ( q % 2 == 0 )
                {
                    rowIndex += 2;
                    centerIndex = baseCenterIndex + rowIndex * _colPointsNum;
                }
                else
                {
                    centerIndex += 2;
                }
                //Debug.Log( "==========================" + centerIndex );
                MoveCardsLogicalTo( _shuffuleAllCards[ q ], centerIndex, 0, false );
                EffectMoveTo( _shuffuleAllCards[ q ], !auto );
            }
        }
        _shuffuleAllCards.Clear();
        _shuffleAllIndex.Clear();
        _shuffleAllLayer.Clear();

        CheckCameraPos();

        UpdateUseableStatus( true );
    }
    #endregion

    #endregion

    #endregion

}
