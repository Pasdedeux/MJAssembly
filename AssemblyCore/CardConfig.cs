using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitFramework;
using DG.Tweening;

public class CardConfig : SingletonMono<CardConfig> 
{
    [Range( 0, 1f )]
    [Header( "洗牌重叠谜之概率" )]
    public float cardProbability = 0.5f;

    [Range( 0, 1f )]
    [Header( "不可选麻将暗度" )]
    public float CardDarkness = 144f / 255f;

    [Header( "麻将移动耗时" )]
    public float CardMoveNormalTime = 0.5f;

    [Space( 20 )]
    [Header( "牌组向外飞出耗时" )]
    public float CardMoveOutTime = 0.25f;
    [Header( "牌组向外飞出幅度" )]
    [Range( 0, 3 )]
    public float CardMoveOutRange = 1.5f;
    [Header( "配对成功时，移动到双方中心点耗时" )]
    public float CardMoveToMiddleTime = 1f;
    //[HideInInspector]
    [Header( "配对成功后重点停留时间" )]
    public float MiddleStayTime = 1f;
    [Header( "配对动画类型" )]
    public Ease EaseType = Ease.OutElastic;

    [Space( 20 )]
    [Header( "Combo时效" )]
    public float ComboTime = 3f;

    [Header( "初始获取的提示次数" )]
    public int HintNum = 3;

    [Header( "初始获取的洗牌次数" )]
    public int ShuffleNum = 3;

    [Header( "颜色渐变时间" )]
    public float colorTransTime = 0.5f;
    public List<Color> ComboColors;


    [Header( "Gang 连消间隔" )]
    public float GangInterval = 0.3f;
    [Header("Gang 动画维持时间")]
    public float GangWaitTime = 1f;
    [Header("Gang 连消最大数量")]
    public int GangMaxNum = 5;

    [Header( "黄色选框动画时间" )]
    public float PickTime = 0.1f;
    [Header( "黄色放大倍数" )]
    public float PickScale = 1.2f;


    public Color GetComboMask(int comboLevel)
    {
        if ( comboLevel > 0 ) return ComboColors[ comboLevel - 1 ];
        else return ComboColors[ 0 ];

    }
}
