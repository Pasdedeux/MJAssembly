using System.Collections.Generic;
using UnityEngine;
using LitFramework;
using LitFramework.Base;
using System;
using LitFramework.Input;
using DG.Tweening;
using System.Linq;
using LitFramework.LitTool;
using System.Collections;
using LitJson;
using LitFramework.Mono;
using Assets.Scripts;

/// <summary>
/// 此类需要拆分
/// </summary>
public partial class CardManager : Singleton<CardManager>, IManager
{

    private GameObject _smog;

    public void Install()
    {
        if ( _cardsRootTrans == null )
        {
            GameObject root = new GameObject( "CardsRoot" );
            _cardsRootTrans = root.transform;

            //麻将仓库
            for ( int i = 0; i < 140; i++ )
                _allBaseCardsQueue.Enqueue( new BaseCard( -1, -1, -1 ) );
        }
        //全部花色
        ALL_COLORS = new int[ ALL_COLORS_NUM ];
        ALL_FLOWERS = new int[ ALL_FLOWERS_NUM ];
        for ( int i = 0; i < ALL_COLORS_NUM; i++ ) ALL_COLORS[ i ] = i;
        for ( int q = 0; q < ALL_FLOWERS_NUM; q++ ) ALL_FLOWERS[ q ] = q + ALL_COLORS_NUM;

        //当前场上麻将牌组
        _currentAllLevelsCards = new Dictionary<int, Dictionary<int, BaseCard>>();
        //当前关卡配置文件存储牌组
        _currentAllCardsConfig = new Dictionary<int, Dictionary<int, BaseCard>>();
        //网格打点数据
        _currentAllLayerDict = new Dictionary<int, Dictionary<int, int>>();
        //当前可用位置的中心索引点字典
        _curCanUseCardIndex = new Dictionary<int, List<int>>();
        //

        _currentColors = new List<int>();
        _currentFlowers = new List<int>();

        _colorsRespDict = new Dictionary<int, int>();
        _flowerRespDict = new Dictionary<int, int>();
        _sortingOrderByIndex = new Dictionary<int, int>();

        _shuffleAllIndex = new List<int>();
        _shuffleAllLayer = new List<int>();
        _shuffuleAllCards = new List<BaseCard>();
        _gangWaitCoroutTime = new WaitForSeconds( 0.3f );

        //todo 用于Demo版本
        _allCenterUseableDict = new List<int>();

        //checked obj
        _checkedObj = SpawnManager.Instance.SpawnObject( SpwanType.BaseCards, -2 ).transform;
        _checkedObj.GetComponent<SpriteRenderer>().sortingOrder = 20000;
        _checkedObj.position = Vector3.one * 100;

        //hint obj
        _hintsObj1 = SpawnManager.Instance.SpawnObject( SpwanType.BaseCards, -3 ).transform;
        _hintsObj1.GetComponent<SpriteRenderer>().sortingOrder = 20000;
        _hintsObj1.position = Vector3.one * 100;
        _hintsObj2 = SpawnManager.Instance.SpawnObject( SpwanType.BaseCards, -3 ).transform;
        _hintsObj2.GetComponent<SpriteRenderer>().sortingOrder = 20000;
        _hintsObj2.position = Vector3.one * 100;

        //特效忌讳用setactive....
        _smog = SpawnManager.Instance.SpawnObject( SpwanType.Effect, -4 );
        _smog.transform.position = Vector3.one * 999;
        _smog.SetActive( false );

        _cardConfig = CardConfig.Instance;

        InputControlManager.Instance.TouchEndCallback += OnTouchCardsEnd;
        GameManager.Instance.DelMainUpdate += UpdateEngine;
    }

    public void Uninstall()
    {
        InputControlManager.Instance.TouchEndCallback -= OnTouchCardsEnd;
        //当前场上麻将牌组
        _currentAllLevelsCards.Clear();
        _currentAllLevelsCards = null;
        //当前关卡配置文件存储牌组
        _currentAllCardsConfig.Clear();
        _currentAllCardsConfig = null;
        //网格打点数据
        _currentAllLayerDict.Clear();
        _currentAllLayerDict = null;
        //当前可用位置的中心索引点字典
        _curCanUseCardIndex.Clear();
        _curCanUseCardIndex = null;
        //清楚多余数据
        _allCenterUseableDict.Clear();
        _allCenterUseableDict = null;
        _sortingOrderByIndex.Clear();
        _sortingOrderByIndex = null;
        //shuffle
        _shuffleAllIndex.Clear();
        _shuffleAllLayer.Clear();
        _shuffuleAllCards.Clear();
        _shuffleAllIndex = null;
        _shuffleAllLayer = null;
        _shuffuleAllCards = null;

        _currentColors.Clear();
        _currentFlowers.Clear();
        _currentColors = null;
        _currentFlowers = null;
        _colorsRespDict.Clear();
        _flowerRespDict.Clear();
        _colorsRespDict = null;
        _flowerRespDict = null;

        ALL_COLORS = null;
        ALL_FLOWERS = null;

        DelCallComboInfo = null;

        if ( _cardMiddleSequence != null ) _cardMiddleSequence.Kill();
        _cardMiddleSequence = null;

        if ( _cardsRootTrans != null )
        {
            GameObject.DestroyImmediate( _cardsRootTrans.gameObject );
            _allBaseCardsQueue.Clear();
            GC.Collect();
        }
    }

    public void UpdateEngine()
    {
        if ( _comboLeftTime >= 0 )
        {
            _comboLeftTime -= Time.deltaTime;
            if ( _comboLeftTime < 0 )
            {
                EndCombo();
            }
        }
    }

    #region 表现效果
    private Sequence _cardMiddleSequence;
    private bool _showHints = false;
    /// <summary>
    /// 展示提示效果
    /// </summary>
    public void ShowHintsEffect()
    {
        AudioManager.Instance.PlaySE( DataModel.Sound.Sound_HINT );

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
            }
        }

        List<int> keys = canUsePairCards.Keys.ToList();
        List<BaseCard> baseCards;
        do
        {
            int ids = keys[ UnityEngine.Random.Range( 0, keys.Count ) ];
            baseCards = canUsePairCards[ ids ];
        }
        while ( baseCards.Count < 2 );

        _hintsObj1.position = baseCards[0].cardObj.transform.position;
        _hintsObj1.GetComponent<SpriteRenderer>().sortingOrder = AssignSortingOrder( baseCards[ 0 ] ) + 1;
        _hintsObj2.position = baseCards[ 1 ].cardObj.transform.position;
        _hintsObj2.GetComponent<SpriteRenderer>().sortingOrder = AssignSortingOrder( baseCards[ 1 ] ) + 1;

        _showHints = true;
    }

    /// <summary>
    /// 关闭提示效果
    /// </summary>
    private void HideHintsEffect()
    {
        if ( _showHints )
        {
            _hintsObj1.position = Vector3.one * 999;
            _hintsObj2.position = Vector3.one * 999;
            _hintsObj1.GetComponent<SpriteRenderer>().sortingOrder = 20000;
            _hintsObj2.GetComponent<SpriteRenderer>().sortingOrder = 20000;

            _showHints = false;
        }
        
    }

    private void EffectMatchFail( BaseCard curBaseCard )
    {
        //if ( DataModel.Instance.GetUseVibrate() )
        //    Handheld.Vibrate();

        EffectSetCardCount( curBaseCard );
        EffectSetCardCount( _lastTouchedCards );

        //_lastTouchedCards = null;
    }


    #region 震动效果
    /// <summary>
    /// 设置单张牌的震动效果
    /// </summary>
    /// <param name="baseCard">Base card.</param>
    private void EffectSetCardCount( BaseCard baseCard )
    {
        //_camCurrentPos = _mainCam.transform.position;
        //_shakeCount = Random.Range(10, 20);
    }
    /// <summary>
    /// 实际震动逻辑，目前需要绑定到update 上执行
    /// </summary>
    private void EffectShakeCard()
    {
        //if (_shakeCount > 0)
        //{
        //    _shakeCount--;
        //    float r = Random.Range(-camShakeRadio, camShakeRadio);//随机的震动幅度
        //    if (_shakeCount == 0)
        //        //保证最终回归到原始位置
        //        _mainCam.transform.position = _camCurrentPos;
        //    else
        //        _mainCam.transform.position = _camCurrentPos + Vector3.one * r;
        //}

    }
    #endregion

    /// <summary>
    /// 指定两张牌移动到两者之间的位置，放大，最后移动到屏幕游戏右下角，缩小
    /// </summary>
    /// <param name="baseCard1">Base card1.</param>
    /// <param name="baseCard2">Base card2.</param>
    private void EffectMoveToMiddlePoint( BaseCard baseCard1, BaseCard baseCard2 , Action action = null )
    {
        Debug.Log( "移动到两点间的位置" );
        float moveTime = _cardConfig.CardMoveNormalTime;
        float middleMoveTime = _cardConfig.CardMoveToMiddleTime;

        Sequence sq;
        if ( action == null )
        {
            sq = _cardMiddleSequence;
            if ( sq != null )
            {
                sq.Complete();
                sq.Kill();
                sq = null;
            }
        }

        sq = DOTween.Sequence();
        sq.PrependCallback( () => { _canPlayBack = false;/*InputControlManager.Instance.IsEnable = false;*/ } );

        GameObject obj1 = baseCard1.cardObj;
        GameObject obj2 = baseCard2.cardObj;

        Vector3 pos1 = obj1.transform.position;
        Vector3 pos2 = obj2.transform.position;
        Vector3 targetPosLeft = ( pos1 + pos2 ) * 0.5f + Vector3.left * CARDS_HALFWIDTH;
        Vector3 targetPosRight = ( pos1 + pos2 ) * 0.5f + Vector3.right * CARDS_HALFWIDTH;

        TweenCallback getMiddleFunc = () => 
        {
            _smog.transform.position = ( targetPosLeft + targetPosRight ) * 0.5f;
            _smog.SetActive( true );
            LitTool.DelayPlayFunction( 1f, () =>
            {
                _smog.transform.position = Vector3.one * 999;
                _smog.SetActive( false );
            } );
        }; 

        //移动到中心点
        if ( pos1.x <= pos2.x )
        {
            baseCard1.sprite.sortingOrder = 10000 - 1;
            baseCard2.sprite.sortingOrder = 10000 + 1;

            sq.Join( obj1.transform.DOMove( targetPosLeft + Vector3.left * _cardConfig.CardMoveOutRange, _cardConfig.CardMoveOutTime ).SetEase( Ease.OutQuint ));
            sq.Join( obj2.transform.DOMove( targetPosRight + Vector3.right * _cardConfig.CardMoveOutRange, _cardConfig.CardMoveOutTime ).SetEase( Ease.OutQuint ) );

            sq.AppendInterval( _cardConfig.CardMoveOutTime );

            sq.Join( obj1.transform.DOMove( targetPosLeft, middleMoveTime ).SetEase( _cardConfig.EaseType ) );
            sq.Join( obj2.transform.DOMove( targetPosRight, middleMoveTime ).SetEase( _cardConfig.EaseType ).OnComplete( getMiddleFunc ) );  //reache call Back
        }
        else
        {
            baseCard1.sprite.sortingOrder = 10000 + 1;
            baseCard2.sprite.sortingOrder = 10000 - 1;

            sq.Join( obj1.transform.DOMove( targetPosRight + Vector3.right * _cardConfig.CardMoveOutRange, _cardConfig.CardMoveOutTime ).SetEase( Ease.OutQuint ) );
            sq.Join( obj2.transform.DOMove( targetPosLeft + Vector3.left * _cardConfig.CardMoveOutRange, _cardConfig.CardMoveOutTime ).SetEase( Ease.OutQuint ) );

            sq.AppendInterval( _cardConfig.CardMoveOutTime );

            sq.Join( obj1.transform.DOMove( targetPosRight, middleMoveTime ).SetEase( _cardConfig.EaseType ) );
            sq.Join( obj2.transform.DOMove( targetPosLeft, middleMoveTime ).SetEase( _cardConfig.EaseType ).OnComplete( getMiddleFunc ) );   //reache call Back
        }
        //_cardMiddleSequence.Join( obj1.transform.DOScale( Vector3.one * 1.1f, middleMoveTime * 0.5f ).SetEase( Ease.Linear ) );
        //_cardMiddleSequence.Join( obj2.transform.DOScale( Vector3.one * 1.1f, middleMoveTime * 0.5f ).SetEase( Ease.Linear ) );

        //中心点等待
        sq.AppendInterval( _cardConfig.MiddleStayTime );

        sq.Join( baseCard2.sprite.material.DOFade( 0f, moveTime ) );
        sq.Join( baseCard1.sprite.material.DOFade( 0f, moveTime ) );

        sq.Join( obj1.transform.DOScale( Vector3.one * 0.5f, moveTime ).SetEase( Ease.Linear ) );
        sq.Join( obj2.transform.DOScale( Vector3.one * 0.5f, moveTime ).SetEase( Ease.Linear ) );

        //结束
        sq.AppendCallback( () => { _canPlayBack = true; CheckCameraPos(); if ( action != null ) action(); sq.Kill();  /*baseCard1.cardObj.SetActive( false ); baseCard2.cardObj.SetActive( false );*/ } );
    }


    private void StopEffect()
    {
        InputControlManager.Instance.IsEnable = true;
        if ( _cardMiddleSequence != null )
        {
            _cardMiddleSequence.Complete();
            _cardMiddleSequence.Kill();
            _cardMiddleSequence = null;
        }
    }

    /// <summary>
    /// 移动牌到指定索引及层
    /// </summary>
    private void EffectMoveTo( BaseCard baseCards, bool showEffect = true )
    {
        //计算将要前往的目标位置
        Vector3 pos = new Vector3(
            CARDS_HALFWIDTH * baseCards.ColIndex - baseCards.Layer * CARDS_XOFFSET,
            CARDS_HALFHEIGHT * baseCards.RowIndex * -1f + baseCards.Layer * CARDS_YOFFSET,
            -0.05f * baseCards.Index - 1 * baseCards.Layer );

        baseCards.sprite.material.DOFade( 1, 0.1f );
        if ( showEffect )
        {
            var cardMiddleSequence = DOTween.Sequence();
            cardMiddleSequence.PrependCallback( () => { baseCards.sprite.sortingOrder = 10000; } );
            cardMiddleSequence.Append( baseCards.cardObj.transform.DOLocalMove( pos, _cardConfig.CardMoveNormalTime ) );
            cardMiddleSequence.Join( baseCards.cardObj.transform.DOScale( Vector3.one, _cardConfig.CardMoveNormalTime ) );
            cardMiddleSequence.AppendCallback( () => { baseCards.sprite.sortingOrder = AssignSortingOrder( baseCards ); } );
            cardMiddleSequence.OnComplete( () => { CheckCameraPos(); cardMiddleSequence.Kill(); } );
        }
        else
        {
            baseCards.cardObj.transform.localPosition = pos;
            baseCards.cardObj.transform.localScale = Vector3.one;
            baseCards.sprite.sortingOrder = AssignSortingOrder( baseCards );
        }

    }


    /// <summary>
    /// 根据状态更新排面显示效果
    /// </summary>
    /// <param name="value">Value.</param>
    private void EffectUpdateCardsStatus( BaseCard value, bool terminate = false )
    {
        Color color = new Color();
        color.a = color.r = color.g = color.b = 1f;
        if ( !terminate )
        {
            switch ( value.Status )
            {
                case CardStatus.CanUse:
                    color.r = color.g = color.b = 1f;
                    break;
                case CardStatus.Fixed:
                    color = _cardConfig.GetComboMask( 4 );// DataModel.Instance.ComboLevel );
                    break;
                case CardStatus.UnUseable:

                    break;
                case CardStatus.Used:

                    break;
                case CardStatus.Wait:

                    break;
                default:
                    break;
            }
        }
        value.sprite.DOColor( color, _cardConfig.colorTransTime );
    }

    #endregion

    #region 点击操作、配对、消除、回退等
    private BaseCard _lastTouchedCards;

    private RaycastHit _ray;
    private void OnTouchCardsEnd( Vector2 obj )
    {
        if ( InputControlManager.Instance.CurrentIsOnUI ) return;

        Ray ray = Camera.main.ScreenPointToRay( obj );
        //non alloc has something wrong with it...
        if ( Physics.Raycast( ray, out _ray ) )
        {
            Debug.Log( _ray.transform.name );

            string[] target = _ray.transform.name.Split( '_' );
            int layer = int.Parse( target[ 0 ] );
            int index = int.Parse( target[ 1 ] );

            BaseCard touchedCard;
            try
            {
                touchedCard = _currentAllLevelsCards[ layer ][ index ];
            }
            catch ( Exception ex )
            {
                Checked( false, 20000, Vector3.zero );
                _lastTouchedCards = null;
                return;
            }

            HideHintsEffect();

            if ( touchedCard == null || touchedCard.Status != CardStatus.CanUse || touchedCard == _lastTouchedCards )
            {
                Debug.Log( "<color=red>配对失败，取消</color>" );
                AudioManager.Instance.PlaySE( DataModel.Sound.Sound_NOCHOOSE );
                Checked( false, 20000, Vector3.zero );

                _lastTouchedCards = null;
                return;
            }
            if ( _lastTouchedCards == null )
            {
                _lastTouchedCards = _currentAllLevelsCards[ layer ][ index ];
                Checked( true, AssignSortingOrder( _lastTouchedCards ) + 1, _lastTouchedCards.cardObj.transform.position );
            }
            else
            {
                PickPair( _currentAllLevelsCards[ layer ][ index ] );
            }
        }
    }


    #endregion

}
