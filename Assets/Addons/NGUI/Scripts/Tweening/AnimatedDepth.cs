/*
 * @Author: Chen ZiLi
 * @Date: 2022/05/12 11:38
 * @Description: 动态变化的UI深度
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedDepth : MonoBehaviour
{
	class UIWidgetInfo
	{
		public UIWidget widget;
		public int originDepth;
	}
	
	/// <summary>
	/// 深度偏移量
	/// </summary>
	public int depthOffset;

	/// <summary>
	/// 当前偏移量
	/// </summary>
	private int _curOffset;

	private List<UIWidgetInfo> _widgetList;

	// Use this for initialization
	void OnEnable ()
	{
		_widgetList = new List<UIWidgetInfo>();
		UIWidget[] uiWidgets = gameObject.GetComponentsInChildren<UIWidget>();
		foreach (UIWidget temp in uiWidgets)
		{
			UIWidgetInfo info = new UIWidgetInfo();
			info.widget = temp;
			info.originDepth = temp.depth;
			_widgetList.Add(info);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (_curOffset != depthOffset)
		{
			_curOffset = depthOffset;
			foreach (UIWidgetInfo info in _widgetList)
			{
				info.widget.depth = info.originDepth + depthOffset;
			}
		}
	}
}
