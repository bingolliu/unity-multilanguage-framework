using UnityEngine;
using System.Collections;

public class MyUICenterOnChild : UICenterOnChild
{
    public delegate void OnFinishedCallback(string name);
    public OnFinishedCallback onFinishedHandler;
    public OnFinishedCallback onPressedHandler;

    // Use this for initialization
    void Start()
    {
        Recenter();
        onFinished = _OnFinished;
    }

    void _OnFinished()
    {
        if (onFinishedHandler != null && centeredObject != null)
        {
            onFinishedHandler(centeredObject.name);
        }
        
    }

    void _onPressed()
    {        
        if (onPressedHandler != null && centeredObject != null)
        {
            onPressedHandler(centeredObject.name);
        }
    }

    

    

}
