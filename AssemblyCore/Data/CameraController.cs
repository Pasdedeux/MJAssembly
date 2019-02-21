#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：Assets.Scripts
* 项目描述 ：
* 类 名 称 ：CameraController
* 类 描 述 ：
* 所在的域 ：DEREK-SURFACE
* 命名空间 ：Assets.Scripts
* 机器名称 ：DEREK-SURFACE 
* CLR 版本 ：4.0.30319.42000
* 作    者 ：lc1027
* 创建时间 ：2018/7/15 14:18:55
* 更新时间 ：2018/7/15 14:18:55
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ lc1027 2018. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    using LitFramework;
    using UnityEngine;
    using LitFramework.LitTool;
    public class CameraController : SingletonMono<CameraController>
    {
        [Range(0,0.5f)]
        public float camShakeRadio = 0.2f;

        [Header("水平方向向内收缩比例")]
        [Range(0,1f)]
        public float xOffset = 0.5f;
        [Header("垂直方向向内收缩比例")]
        [Range( 0, 1f )]
        public float yOffset = 0.5f;

        //主摄
        private Camera _mainCam;
        //关注的玩家
        private Transform _playerTrans;

        private float _leftBorder;
        private float _rightBorder;
        private float _topBorder;
        private float _downBorder;
        private float _halfWidth;
        private float _halfHeight;

        private int _shakeCount = 0;
        
        private Vector3 _camCurrentPos; //相机的初始位置
        private Vector3 _camTargetPos;//相机目标位置
        private Vector3 _tmpOffset;
        private Vector3[] _camViewBounds;

        public void SetCam( Camera cam )
        {
            if ( _mainCam != null ) return;
            _mainCam = cam;

            _camViewBounds = new Vector3[ 4 ];
            //目标位置即当前位置
            _camTargetPos = _mainCam.transform.position;
            _camCurrentPos = _camTargetPos;
            _targetSize = _mainCam.orthographicSize;
            //世界坐标的右上角  因为视口坐标右上角是1,1,点
            Vector3 cornerPos = Camera.main.ViewportToWorldPoint( new Vector3( 1f, 1f, Mathf.Abs( -Camera.main.transform.position.z ) ) );
            //世界坐标左边界
            _leftBorder = Camera.main.transform.position.x - ( cornerPos.x - Camera.main.transform.position.x );
            //世界坐标右边界
            _rightBorder = cornerPos.x;
            //世界坐标上边界
            _topBorder = cornerPos.y;
            //世界坐标下边界
            _downBorder = Camera.main.transform.position.y - ( cornerPos.y - Camera.main.transform.position.y );

            _halfWidth = ( _rightBorder - _leftBorder ) * 0.5f;
            _halfHeight = ( _topBorder - _downBorder ) * 0.5f;

            _leftBorder += _halfWidth * xOffset;
            _rightBorder -= _halfWidth * xOffset;
            _topBorder -= _halfHeight * yOffset;
            _downBorder += _halfHeight * yOffset;
        }

        public void SetPlayer( Transform player )
        {
            if ( _playerTrans != null ) return;
            _playerTrans = player;
        }

        private void LateUpdate()
        {
            if ( _mainCam != null )
            {
                //插值
                _tmpOffset = GetCameraPos( _camTargetPos );
                _mainCam.transform.position = _tmpOffset;

                _mainCam.orthographicSize = GetCameraSize();

                //干脆
                //_mainCam.transform.position = _playerTrans.position - _camOffset;

                //相机震动需求检测
                ShakeWithCount();
            }
        }

        public void ShakeCameraWithCount()
        {
            _camCurrentPos = _mainCam.transform.position;
            _shakeCount = Random.Range( 10, 20 );

            if ( DataModel.Instance.GetUseVibrate() )
                Handheld.Vibrate();
        }

        void ShakeWithCount()
        {
            if ( _shakeCount > 0 )
            {
                _shakeCount--;
                float r = Random.Range( -camShakeRadio, camShakeRadio );//随机的震动幅度
                if ( _shakeCount == 0 )
                    //保证最终回归到原始位置
                    _mainCam.transform.position = _camCurrentPos;
                else
                    _mainCam.transform.position = _camCurrentPos + Vector3.one * r;
            }
        }

        /// <summary>
        /// 获取摄像机实际位置
        /// </summary>
        /// <returns></returns>
        private Vector3 GetCameraPos( Vector3 camPos )
        {
            //camera bound rule
            //Vector3 camPos = _mainCam.transform.position;

            //角色角度跟踪
            //Vector3 playerPos = _playerTrans.position;
            //camPos.x = Mathf.Max( Mathf.Min( playerPos.x, _rightBorder ), _leftBorder );
            //camPos.y = Mathf.Max( Mathf.Min( playerPos.y, _topBorder ), _downBorder );

            return Vector3.Lerp(_mainCam.transform.position, camPos, 3f * Time.deltaTime);
        }


        private float GetCameraSize()
        {
            //camera bound rule
            //Vector3 camPos = _mainCam.transform.position;

            //角色角度跟踪
            //Vector3 playerPos = _playerTrans.position;
            //camPos.x = Mathf.Max( Mathf.Min( playerPos.x, _rightBorder ), _leftBorder );
            //camPos.y = Mathf.Max( Mathf.Min( playerPos.y, _topBorder ), _downBorder );

            return Mathf.Lerp( _mainCam.orthographicSize, _targetSize, 3f * Time.deltaTime );
        }


        #region 镜头移动相关

        public void ResetCamera()
        {
            _mainCam.orthographicSize = 10;
            _mainCam.transform.position = Vector3.zero;
            _mainCam.transform.position += Vector3.forward * -100;
        }

        private float _targetSize;
        public void MoveCamera(Vector3 pos, float sizeRatio )
        {
            _camTargetPos = pos;
            _targetSize = Mathf.Max(6.5f, _mainCam.orthographicSize * sizeRatio+2.5f);

            _mainCam.GetCameraBounds( ref _camViewBounds );
        }

        public void MoveCamera( Vector3 pos )
        {
            _camTargetPos = pos;
            _mainCam.GetCameraBounds( ref _camViewBounds );
        }


        public Vector3[] GetEffectBounds()
        {
            _mainCam.GetCameraBounds( ref _camViewBounds );
            _camViewBounds[ 0 ] += Vector3.right * 1;
            _camViewBounds[ 0 ] -= Vector3.up * 2;

            _camViewBounds[ 1 ] -= Vector3.right * 1;
            _camViewBounds[ 1 ] -= Vector3.up * 2;

            _camViewBounds[ 2 ] += Vector3.right * 1;
            _camViewBounds[ 2 ] += Vector3.up * 2;

            _camViewBounds[ 3 ] -= Vector3.right * 1;
            _camViewBounds[ 3 ] += Vector3.up * 2;

            return _camViewBounds;
        }



        #endregion
    }
}