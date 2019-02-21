using UnityEngine;
using System.Collections;
using LitFramework;
using LitFramework.Base;
using LitFramework.Mono;

public class UIProxy : Singleton<UIProxy>, IManager
{
    private UIManager _uiMgr;

    public void Install()
    {
        _uiMgr = UIManager.Instance;
        _uiMgr.Install();
        _uiMgr.LoadResourceFunc = (e) => { return Resources.Load(e) as GameObject; };
    }

    public void Uninstall()
    {
        _uiMgr.Uninstall();
    }
}
