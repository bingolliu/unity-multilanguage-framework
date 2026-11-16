//----------------------------------------------
// NGUI:UIScrollView Extension by Ransongsong
//----------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This scipts is extension of the UIScrollView
/// for use child item in loop, and save the memory and perfermance
/// ATTENTION : support the grid!!!!!!!!!!!!!!!
/// 例子如下：
/// UIScrollViewEx ex = GetComponent<UIScrollViewEx>();
/// ex.SetData(_gItemPrefab,19,12, (GameObject item, int index, int itemIndex) =>
///        {
///            item.GetChildByName("Label").GetComponent<UILabel>().text = index.ToString();
///        });
/// attention: 复用列表不能超过1000行。超过的话会报错。
/// 对于lua中，因为需要脚本无法绑定在 item gameobject上，
/// 故需要：
/// 1.setdata
/// 2.create child callback : then get all items object
/// 3.ResetPostion
/// </summary>


[RequireComponent(typeof(UIScrollView))]
[AddComponentMenu("NGUI/Interaction/Scroll View Ex")]
public class UIScrollViewEx : MonoBehaviour
{

    /// <summary>
    /// call when the scroll view data changed
    /// </summary>
    /// <param name="item">the need updated item</param>
    /// <param name="dataIndex">the data idx</param>
    /// <param name="itemIndex">the item idx in its parent child</param>
    public delegate void ScrollViewDataChangeUpdate(GameObject item, int dataIndex, int itemIndex);

    /// <summary>
    /// when create child success,call it
    /// </summary>
    /// <param name="childs"></param>
    public delegate void ScrollViewItemCreateDelForLua(Transform[] childs);

    #region Public Variable

    /// <summary>
    /// 网格布局size,例如(5,4)=>网格为5*4
    /// </summary>
    public Vector2 _gridSize;

    /// <summary>
    /// item size
    /// </summary>
    public Vector2 _itemSize;

    /// <summary>
    /// item space between
    /// </summary>
    public Vector2 _spaceSize;

    /// <summary>
    /// auto moving speed
    /// </summary>
    public float _fAutoVelocity = 8f;

    /// <summary>
    /// 能否移过边界
    /// </summary>
    public bool _bMoveBeyondBound = false;

    public GameObject _scrollViewItem;

    #endregion

    #region Private Variable

    /// <summary>
    /// the del when item changed and update data
    /// </summary>
    private ScrollViewDataChangeUpdate _dataChangedUpdateDel;

    /// <summary>
    /// the del when create child items for lua
    /// </summary>
    private ScrollViewItemCreateDelForLua _itemCreatedDelForLua;

    /// <summary>
    /// max nums of child item
    /// </summary>
    private int _iMaxNums;

    /// <summary>
    /// current item number
    /// </summary>
    private int _iCurrentNums;

    /// <summary>
    /// the item prefab
    /// </summary>
    private GameObject _gItemPrefab;

    /// <summary>
    /// it's the data idx of the font item
    /// </summary>
    private int _iCurrentItemIdx = 0;

    /// <summary>
    /// scrollview panel position , it's used for comparing with now
    /// </summary>
    private Vector3 _panelPos = Vector3.zero;

    /// <summary>
    /// the scollview
    /// </summary>
    private UIScrollView _scrollView;

    /// <summary>
    /// the panel
    /// </summary>
    private UIPanel _panel;

    private Vector3 _panelOrgLocalPos;

    /// <summary>
    /// the root of all items
    /// </summary>
    private Transform _tItemRoot;

    /// <summary>
    /// all items
    /// </summary>
    private List<Transform> _itemList = new List<Transform>();

    public bool isAuto = false;
    public int maxNum = 0;
    public int curNum = 0;
    /// <summary>
    /// the position of each item
    /// </summary>
    private List<Vector3> _posList = new List<Vector3>();
    private int[] dataIndexMap = new int[1000];
    #endregion

    void Awake()
    {
        _scrollView = this.GetComponent<UIScrollView>();
        _panel = this.GetComponent<UIPanel>();
        _tItemRoot = transform.Find("ROOT");
        if (_tItemRoot == null)
        {
            GameObject obj = new GameObject();
            obj.name = "ROOT";
            obj.transform.SetParent(transform);
            obj.transform.localScale = Vector3.one;
            if (_scrollView.movement == UIScrollView.Movement.Horizontal)
                obj.transform.localPosition = new Vector3(-_panel.GetViewSize().x / 2, 0, 0);
            else if (_scrollView.movement == UIScrollView.Movement.Vertical)
                obj.transform.localPosition = new Vector3(0, _panel.GetViewSize().y / 2, 0);
            _tItemRoot = obj.transform;
        }
        else
        {
            if (_scrollView.movement == UIScrollView.Movement.Horizontal)
                _tItemRoot.localPosition = new Vector3(-_panel.GetViewSize().x / 2, 0, 0);
            else if (_scrollView.movement == UIScrollView.Movement.Vertical)
                _tItemRoot.localPosition = new Vector3(0, _panel.GetViewSize().y / 2, 0);
        }
    }

    void Start()
    {
        if (isAuto)
        {
            SetData(_gItemPrefab, maxNum, curNum, null);
        }
    }

    void LateUpdate()
    {
        Vector3 pos = _panel.transform.localPosition;
        if (pos.Equals(_panelPos))
            return;
        _panelPos = pos;

        int itemIdx = 0;
        int gridIdx = 0;
        int maxGridIdx = 0;

        //caculate the distance has moved
        if (_scrollView.movement == UIScrollView.Movement.Horizontal)
        {
            gridIdx = ((int)(-_panelPos.x / (_itemSize.x + _spaceSize.x)));
            itemIdx = gridIdx * (int)_gridSize.x;
            maxGridIdx = Mathf.CeilToInt((_iMaxNums - _iCurrentNums) / _gridSize.x);
        }
        else if (_scrollView.movement == UIScrollView.Movement.Vertical)
        {
            gridIdx = ((int)(_panelPos.y / (_itemSize.y + _spaceSize.y)));
            itemIdx = gridIdx * (int)_gridSize.y;
            maxGridIdx = Mathf.CeilToInt((_iMaxNums - _iCurrentNums) / _gridSize.y);
        }

        itemIdx = itemIdx < 0 ? 0 : itemIdx;
        //not enough a grid
        if (itemIdx == _iCurrentItemIdx)
        {
            return;
        }

        if (gridIdx > maxGridIdx)
        {
            return;
        }
        ResetPostion(itemIdx, false);
    }

    #region Public Methods

    /// <summary>
    /// 设置ScrollViewEx数据唯一接口
    /// </summary>
    /// <param name="itemPrefab"></param>
    /// <param name="maxNums"></param>
    /// <param name="currentNums"></param>
    /// <param name="del"></param>
    public void SetData(GameObject itemPrefab, int maxNums, int currentNums, ScrollViewDataChangeUpdate del)
    {
        _gItemPrefab = itemPrefab;
        if (_scrollViewItem != null)
        {
            // set prefab 
            // Debug.Log("Set prefab");
            _gItemPrefab = _scrollViewItem;
            _gItemPrefab.SetActive(false);
        }
        _iMaxNums = maxNums;
        _iCurrentNums = currentNums;
        _dataChangedUpdateDel = del;
        _iCurrentItemIdx = 0;

        StartCoroutine("Init");
    }

    /// <summary>
    /// 设置ScrollViewEx数据唯一接口 for Lua
    /// </summary>
    /// <param name="itemPrefab"></param>
    /// <param name="maxNums"></param>
    /// <param name="currentNums"></param>
    /// <param name="del"></param>
    /// <param name="delForLua"></param>
    public void SetData(GameObject itemPrefab, int maxNums, int currentNums, ScrollViewDataChangeUpdate del,
        ScrollViewItemCreateDelForLua delForLua)
    {
     
        _itemCreatedDelForLua = delForLua;
        SetData(itemPrefab, maxNums, currentNums, del);
    }

    public void ResetPostion(int idx = 0)
    {
        ResetPostion(idx, true);
    }

    /// <summary>
    /// 1.reset the position of all items
    /// 2.update all items data
    /// </summary>
    /// <param name="idx"></param>
    public void ResetPostion(int idx, bool isInit)
    {
        int count = _itemList.Count;
        for (int i = 0; i < count; i++)
        {
            if (i >= _iMaxNums)
            {
                // SetDisablePosition(_itemList[i]);
                _itemList[i].gameObject.SetActive(false);
                continue;
            }
            if (isInit)
            {
                _itemList[i].localPosition = _posList[i + idx];
                //_itemList[i].gameObject.name = "Item" + (i) + "-" + (i + idx);
                dataIndexMap[i] = i + idx;
            }
            _itemList[i].gameObject.SetActive(true);
        }

        if (_iCurrentItemIdx > idx && idx + count <= _iMaxNums)
        {
            //往下或者右滑动
            for (int i = _iCurrentItemIdx - 1; i >= idx && i >= 0; i--)
            {
                int indexItem = (i + count) % count;
                // Debug.Log("=-=-=i-=-" + i + "   - - " + indexItem);
                _itemList[indexItem].localPosition = _posList[i];
                _itemList[indexItem].name = "Item" + indexItem + "-" + i;
                dataIndexMap[indexItem] = i;
                if (_dataChangedUpdateDel != null)
                {
                    _dataChangedUpdateDel(_itemList[indexItem].gameObject, i, indexItem);
                }
            }
            _iCurrentItemIdx = idx;

        }
        else if (_iCurrentItemIdx < idx && count + _iCurrentItemIdx <= _iMaxNums)
        {
            //往上或者左移动
            for (int i = _iCurrentItemIdx; (i < idx) && (count + i < _iMaxNums); i++)
            {
                int indexItem = i % count;
                // Debug.Log("=-=-=i up-=-  " + i + "  " + indexItem + "  " + count);
                int dataIndex = count + i;
                _itemList[indexItem].localPosition = _posList[dataIndex];
                _itemList[indexItem].name = "Item" + indexItem + "-" + (dataIndex);
                dataIndexMap[indexItem] = dataIndex;
                if (_dataChangedUpdateDel != null)
                {
                    _dataChangedUpdateDel(_itemList[indexItem].gameObject, count + i, indexItem);
                }
            }
            _iCurrentItemIdx = idx;
        }

    }

    // public void 

    /// <summary>
    /// 1.update the max nums
    /// 2.reset the position of all items
    /// 3.update all items data
    /// </summary>
    /// <param name="maxNums"></param>
    /// <param name="idx"></param>
    public void ResetNumAndPosition(int maxNums, int idx)
    {
        _iMaxNums = maxNums;
        _iCurrentItemIdx = idx;
        InitPositionData();
        UpdateItem();
        ResetPostion(idx);
        _scrollView.ResetPosition();

    }

    /// <summary>
    /// update the all items base on currentitemidx
    /// dont reset the position
    /// </summary>
    public void UpdateItem()
    { 
        int index = 0;
        int count = _itemList.Count;
        for (int i = 0; i < count; i++)
        {
            if (i >= _iMaxNums)
            {
                SetDisablePosition(_itemList[i]);
                _itemList[i].gameObject.SetActive(false);
            }
            else
            {
                _itemList[i].gameObject.SetActive(true);
                if (_dataChangedUpdateDel != null)
                {
                    _dataChangedUpdateDel(_itemList[i].gameObject, dataIndexMap[i], i);

                }
            }
            //_itemList[i].gameObject.name = "Item" + i + "-" + dataIndexMap[i];

        }
    }

    /// <summary>
    /// reset the max number
    /// </summary>
    /// <param name="maxNums"></param>
    public void UpdateItem(int maxNums)
    {
        if (maxNums < _iMaxNums)
        {
            ResetNumAndPosition(maxNums, 0);
        }
        else
        {
            _iMaxNums = maxNums;
            InitPositionData();
            UpdateItem();
        }
    }

    /// <summary>
    /// move to the idx in row or col
    /// </summary>
    /// <param name="idx">Horizontal:coloum idx ; Vertical:row idx</param>
    public void GotoIndex(int idx)
    {
        if (idx < 0)
            return;

        Vector3 targetPos = Vector3.zero;

        //借助子item位置找到panel对应item idx时位置
        if (_scrollView.movement == UIScrollView.Movement.Horizontal)
        {
            targetPos = new Vector3(-_posList[idx * (int)_gridSize.x].x + _itemSize.x / 2, _panelPos.y, _panelPos.z);
        }
        else if (_scrollView.movement == UIScrollView.Movement.Vertical)
        {
            targetPos = new Vector3(_panelPos.x, -_posList[idx * (int)_gridSize.y].y - _itemSize.y / 2, _panelPos.z);
        }

        SpringPanel.Begin(_panel.cachedGameObject, targetPos, _fAutoVelocity);
    }

    /// <summary>
    /// change the scrollView clipsize and offset
    /// </summary>
    /// <param name="clipSize">clipSize of Panel</param>
    /// <param name="offset">offset of panel</param>
    public void ChangeSize(Vector4 clipSize, Vector2 offset)
    {
        _panel.baseClipRegion = clipSize;
        _panel.clipOffset = offset;

        //size changed , the root position changed too
        if (_scrollView.movement == UIScrollView.Movement.Horizontal)
            _tItemRoot.localPosition = new Vector3(-_panel.GetViewSize().x / 2, 0, 0);
        else if (_scrollView.movement == UIScrollView.Movement.Vertical)
            _tItemRoot.localPosition = new Vector3(0, _panel.GetViewSize().y / 2, 0);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 初始化
    /// </summary>
    private IEnumerator Init()
    {
        //init setting
        InitPositionData();

        //create
        yield return StartCoroutine(CreateItems());

        if (_itemCreatedDelForLua != null)
            _itemCreatedDelForLua(_itemList.ToArray());

        //position
        ResetPostion(0);
        UpdateItem();
        yield return null;

        if (_scrollView.movement == UIScrollView.Movement.Horizontal)
            _scrollView.contentPivot = UIWidget.Pivot.Left;
        else
            _scrollView.contentPivot = UIWidget.Pivot.Top;


        _scrollView.ResetPosition();
        _panelOrgLocalPos = _panel.transform.localPosition;

    }

    /// <summary>
    /// 初始化position data of all items
    /// </summary>
    private void InitPositionData()
    {
        _posList.Clear();

        for (int i = 0; i < _iMaxNums; i++)
        {
            float x = 0f;
            float y = 0f;

            if (_scrollView.movement == UIScrollView.Movement.Horizontal)
            {
                int rowIdx = i % (int)_gridSize.x;

                int colIdx = Mathf.FloorToInt(i / _gridSize.x);

                x = colIdx * (_itemSize.x + _spaceSize.x) + _itemSize.x / 2;

                y = (rowIdx - (_gridSize.x - 1) / 2) * (_itemSize.y + _spaceSize.y);
            }
            else if (_scrollView.movement == UIScrollView.Movement.Vertical)
            {
                int rowIdx = Mathf.FloorToInt(i / _gridSize.y);

                int colIdx = i % (int)_gridSize.y;

                x = (colIdx - (_gridSize.y - 1) / 2) * (_itemSize.x + _spaceSize.x);

                y = -(_itemSize.y + _spaceSize.y) * rowIdx - _itemSize.y / 2;
            }
            else
            {
                int rowIdx = Mathf.FloorToInt(i / _gridSize.y);

                int colIdx = i % (int)_gridSize.y;

                x = (colIdx - (_gridSize.y - 1) / 2) * _itemSize.x;

                y = _itemSize.y * rowIdx + _itemSize.y + _spaceSize.y;
            }

            Vector3 pos = new Vector3(x, y, 0);

            _posList.Add(pos);
        }



    }


    /// <summary>
    /// 创建child
    /// </summary>
    private IEnumerator CreateItems()
    {
        if (_iMaxNums > 0)
        {
            if (_iMaxNums < _iCurrentNums - 1)
            {
                _scrollView.onDragFinished = () => {
                    if (_scrollView.movement == UIScrollView.Movement.Horizontal)
                    {
                        if (_panel.transform.localPosition.x > _panelOrgLocalPos.x)
                        {
                            SpringPanel.Begin(_panel.cachedGameObject, _panelOrgLocalPos, _fAutoVelocity);
                        }
                    }
                    else if (_scrollView.movement == UIScrollView.Movement.Vertical)
                    {
                        if (_panel.transform.localPosition.y < _panelOrgLocalPos.y)
                        {
                            SpringPanel.Begin(_panel.cachedGameObject, _panelOrgLocalPos, _fAutoVelocity);
                        }
                    }
                };
            }

            int needChildItemCount = _iMaxNums > _iCurrentNums ? _iCurrentNums : _iMaxNums;

            int childCount = _itemList.Count;

            _iCurrentNums = childCount > needChildItemCount ? childCount : needChildItemCount;

            for (int i = childCount; i < needChildItemCount; i++)
            {
                GameObject obj = Instantiate(_gItemPrefab, _tItemRoot);
                obj.SetActive(true);
                obj.transform.localScale = _gItemPrefab.transform.localScale;
                obj.transform.localPosition = Vector3.one * 10000;
                _itemList.Add(obj.transform);
                if (i % 10 == 0)
                    yield return null;
            }
        }
    }
    virtual protected void OnDestroy()
    {
        _dataChangedUpdateDel = null;
        _itemCreatedDelForLua = null;
        StopCoroutine("Init");
        int count = _itemList.Count;
        for (int i = 0; i < count; i++)
        {
            Destroy(_itemList[i].gameObject);
        }
    }

    /// <summary>
    /// 清除所有child
    /// </summary>
    private void ClearItems()
    {
        if (_tItemRoot == null)
            return;

        int childCount = _itemList.Count;

        for (int i = 0; i < childCount; i++)
            GameObject.Destroy(_itemList[i].gameObject);

        _itemList.Clear();
    }

    private void SetDisablePosition(Transform trans)
    {
        // if (_scrollView.movement == UIScrollView.Movement.Horizontal)
        //     trans.localPosition = new Vector3(0,10000,0);
        // else if (_scrollView.movement == UIScrollView.Movement.Vertical)
        //     trans.localPosition = new Vector3(10000, 0, 0);
        // else
        //     trans.localPosition = new Vector3(10000, 10000, 0);
    }

    #endregion
}
