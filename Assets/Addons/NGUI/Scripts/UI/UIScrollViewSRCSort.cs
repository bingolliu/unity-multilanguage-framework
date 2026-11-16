using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIScrollViewSRCSort : MonoBehaviour
{
    public AnimationCurve positionCurve;

    private List<GameObject> goList = new List<GameObject>();   // 缓存gameobject列表
    private List<Vector2> goPos = new List<Vector2>();          // 缓存gameobject原始坐标

    private float midPosX = 0f;
    private float midPosY = 0f;             // 中点的Y坐标
    private bool isInit = false;            // 是否初始化
    private bool debugMode = false;         // 是否开启调试log

    private void CustomizedDebug(string input)
    {
        if (debugMode)
        {
            Debug.Log(input);
        }
    }

    private void Update()
    {
        if (isInit) 
        {
            UpdategoList();
        }
        
    }
     // 代码创建动画曲线
    private AnimationCurve CreateAnimationCurve()
    {
        // 定义关键帧数组
        Keyframe[] ks = new Keyframe[2];

        int i = 0;
        while (i < ks.Length)
        {
            // 给每个关键帧赋值  time, value
            ks[i] = new Keyframe(0, 0);
            i++;
        }

        // 设置前一个点进入该关键帧的切线（也就是设置斜率）
        ks[0].inTangent = 2;
        ks[0].outTangent = 2;

        // 设置第 2 关键帧出去时的切线 （也是设置斜率）
        ks[1].inTangent = 0;
        ks[1].outTangent = 0;
        ks[1].time = 1;
        ks[1].value = 1;

        // 通过关键帧数组实例化动画曲线
        AnimationCurve animaCur = new AnimationCurve(ks);

        //设置动画曲线最后一帧的 循环类型
        animaCur.postWrapMode = WrapMode.Clamp;

        //设置动画曲线第一帧的 循环类型
        animaCur.preWrapMode = WrapMode.Clamp;

        // Keyframe k = new Keyframe(2, 2);
        // // // 添加一个动画帧
        // animaCur.AddKey(k);

        // // 移除第 10 个关键帧
        // animaCur.RemoveKey(10);

        // // 设置第 20 个关键帧 的平滑度
        // animaCur.SmoothTangents(1, 0);
        // animaCur.SmoothTangents(0, 0);

        // 根据时间获取动画曲线 time 时间点的 value
        // float time = 1.0f;
        // animaCur.Evaluate(time);

        return animaCur;
    }

    public void SetMidGameObject(GameObject midGo)
    {
        positionCurve = CreateAnimationCurve();
        SetAnimationCurveData(midGo);
    }

    // 外部接口，本例中从Start中调用
    public void SetAnimationCurveData(GameObject midObj)
    {
        if (isInit) return;

        midPosX = midObj.transform.position.x;
        midPosY = midObj.transform.position.y;
        goList.Clear();
        for (int i = 0; i < transform.childCount; ++i)
        {
            goList.Add(transform.GetChild(i).gameObject);
        }

        //InitPositionY();
        InitOriginalPos();
        isInit = true;
    }

    public void OpendebugMode(bool setting)
    {
        debugMode = setting;
    }

    private void InitOriginalPos()
    {
        goPos.Clear();
        for (int i = 0; i < goList.Count; ++i)
        {
            goPos.Add(goList[i].transform.position);
        }
    }

    private void InitPositionY()
    {
        Transform trans = goList[0].transform.GetComponent<Transform>();        // 本例中排列的gameobject高度统一，取第一个gameobject的高度
        float height = trans.localPosition.y;
        Vector3 vec3;
        for (int i = 0; i < goList.Count; ++i)
        {
            vec3 = goList[i].transform.position;
            goList[i].transform.position = new Vector2(vec3.x, vec3.y - i * (height / 100 - 0.3f));     // 偏移量，根据实际显示效果调整
        }
    }

    void UpdategoList()
    {
        for (int i = 0; i < goList.Count; ++i)
        {
            Transform trans = goList[i].transform;
            float value = Mathf.Abs(trans.position.x - midPosX);
            float y = GetPositionY(value);
            //trans.position = new Vector3(goPos[i].x + x - 0.6f, trans.position.y);     // 偏移量，根据实际显示效果调整
            trans.position = new Vector3(trans.position.x, goPos[i].y + y - 0.47f);     // 偏移量，根据实际显示效果调整

            CustomizedDebug("UpdategoList: " + value + ", trans.position.y:" + trans.position.y + ", trans.position.y: " + trans.position.y + ", midPosY: " + midPosY);
        }
    }

    float GetPositionX(float value)
    {
        float lerpIndex = Mathf.Lerp(1, 0, value / 5);                      // 5是经验数值，约为显示区域显示数量 / 2
        float x = positionCurve.Evaluate(lerpIndex * lerpIndex * 0.5f);     // 利用animation curve进行映射
        CustomizedDebug("GetPositionX lerpIndex: " + lerpIndex + ", x: " + x);
        return x;
    }

    float GetPositionY(float value)
    {
        float lerpIndex = Mathf.Lerp(1, 0, value / 5);                      // 5是经验数值，约为显示区域显示数量 / 2
        float y = positionCurve.Evaluate(lerpIndex * lerpIndex * 0.33f);     // 利用animation curve进行映射
        CustomizedDebug("GetPositionY lerpIndex: " + lerpIndex + ", y: " + y);
        return y;
    }

}