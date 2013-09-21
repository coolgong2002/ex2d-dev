﻿// ======================================================================================
// File         : exTextureInfo.cs
// Author       : Wu Jie 
// Last Change  : 02/17/2013 | 21:39:05 PM | Sunday,February
// Description  : 
// ======================================================================================

///////////////////////////////////////////////////////////////////////////////
// usings
///////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ex2D.Detail;

///////////////////////////////////////////////////////////////////////////////
///
/// The texture-info asset
///
///////////////////////////////////////////////////////////////////////////////

public partial class exTextureInfo : ScriptableObject {
    public string rawTextureGUID = "";
    public string rawAtlasGUID = "";
    public Texture2D texture; ///< the atlas or raw texture

    public bool rotated = false; ///< if rotate the texture in atlas 
    public bool trim = false;    ///< if trimmed the texture
    public int trimThreshold = 1; ///< pixels with an alpha value below this value will be consider trimmed 

    // for texture offset
    public int trim_x = 0; ///< the trim offset x of the raw texture in Unity3D texture coordinate. (0,0) start from bottom-left, same as mesh.uv and Texture2D.SetPixel
    public int trim_y = 0; ///< the trim offset y of the raw texture in Unity3D texture coordinate. (0,0) start from bottom-left, same as mesh.uv and Texture2D.SetPixel
    public int rawWidth = 1;
    public int rawHeight = 1;

    public int x = 0; ///< the x in Unity3D texture coordinate. (0,0) start from bottom-left, same as mesh.uv and Texture2D.SetPixel 
    public int y = 0; ///< the y in Unity3D texture coordinate. (0,0) start from bottom-left, same as mesh.uv and Texture2D.SetPixel
    public int width = 1;
    public int height = 1;

    public int borderLeft = 0;
    public int borderRight = 0;
    public int borderTop = 0;
    public int borderBottom = 0;

    public int rotatedWidth {
        get {
            if ( rotated ) return height;
            return width;
        }
    }
    public int rotatedHeight {
        get {
            if ( rotated ) return width;
            return height;
        }
    }
    public bool hasBorder {
        get {
            return borderLeft != 0 || borderRight != 0 || borderTop != 0 || borderBottom != 0;
        }
    }

    [SerializeField]
    private List<int> diceData = new List<int>();   // TODO: use array

    public int diceUnitWidth {  ///< committed value, used for rendering
        get {
            if (diceData != null && diceData.Count > 0) {
                return diceData[0];
            }
            return 0;
        }
    }
    public int diceUnitHeight {  ///< committed value, used for rendering
        get {
            if (diceData != null && diceData.Count > 0) {
                return diceData[1];
            }
            return 0;
        }
    }

    public bool isDiced {
        get {
            if (diceData != null && diceData.Count > 0) {
                return diceData[0] > 0 || diceData[1] > 0;
            }
            return false;
        }
    }

    public DiceEnumerator dices {  ///< get dice enumerator
        get {
            return new DiceEnumerator(diceData, this);    // No GC
        }
    }
    
    public enum DiceType {
        Empty,
        Max,
        Trimmed,
    }

    // ------------------------------------------------------------------ 
    /// The dice data
    // ------------------------------------------------------------------ 

    public struct Dice {            
        public DiceType sizeType;
        public int offset_x;    ///< 当前格子的左下角坐标经过trim后的偏移，为0相当于没有trim
        public int offset_y;    ///< 当前格子的左下角坐标经过trim后的偏移，为0相当于没有trim
        public int width;       ///< 当前格子的宽度
        public int height;      ///< 当前格子的高度
        public int x;           ///< 当前格子在atlas中的UV起始点
        public int y;           ///< 当前格子在atlas中的UV起始点
        public bool rotated;    ///< if rotate the texture in atlas
        public int rotatedWidth {
            get {
                return rotated ? height : width;
            }
        }
        public int rotatedHeight {
            get {
                return rotated ? width : height;
            }
        }
#if UNITY_EDITOR
        public DiceEnumerator enumerator;
        public int trim_x {      ///< 当前格子在rawTexture中的UV起始点
            get {
                int col = enumerator.diceIndex % enumerator.columnCount;
                return enumerator.textureInfo.trim_x + col * enumerator.textureInfo.diceUnitWidth + offset_x;
            }
        }
        public int trim_y {      ///< 当前格子在rawTexture中的UV起始点
            get {
                int row = enumerator.diceIndex / enumerator.columnCount;
                return enumerator.textureInfo.trim_y + row * enumerator.textureInfo.diceUnitHeight + offset_y;
            }
        }
#endif
    }
}

namespace ex2D.Detail {

///////////////////////////////////////////////////////////////////////////////
//
/// The dice data enumerator. Use struct type to avoid GC.
//
///////////////////////////////////////////////////////////////////////////////

public struct DiceEnumerator : IEnumerator<exTextureInfo.Dice>, IEnumerable<exTextureInfo.Dice> {

    // compressing tag
    public const int EMPTY = -1;
    public const int MAX = -2;
    public const int MAX_ROTATED = -3;
    
    private List<int> diceData;
    private int dataIndex;
    private int diceUnitWidth;
    private int diceUnitHeight;
    
#if UNITY_EDITOR
    public int columnCount;
    public int rowCount;
    public int diceIndex;     ///< current dice index
    public exTextureInfo textureInfo;
#endif

    public DiceEnumerator (List<int> _diceData, exTextureInfo _textureInfo) {
        diceData = _diceData;
        diceUnitWidth = _diceData[0];
        diceUnitHeight = _diceData[1];
        dataIndex = -1;
#if UNITY_EDITOR
        textureInfo = _textureInfo;
        exSpriteUtility.GetDicingCount(textureInfo, out columnCount, out rowCount);
        diceIndex = -1;
#endif
        Reset();
    }

    public IEnumerator<exTextureInfo.Dice> GetEnumerator () { return this; }
    IEnumerator IEnumerable.GetEnumerator () { return this; }

    public static void AddDiceData ( exTextureInfo _textureInfo, List<int> _diceData, exTextureInfo.Dice _dice ) {
        //Debug.Log("rect " + _rect + " " + _x + " " + _y);
        if ( _dice.width <= 0 || _dice.height <= 0 ) {
            _diceData.Add( DiceEnumerator.EMPTY );
        }
        else {
            if ( _dice.width == _textureInfo.diceUnitWidth && _dice.height == _textureInfo.diceUnitHeight ) {
                _diceData.Add( _dice.rotated ? DiceEnumerator.MAX_ROTATED : DiceEnumerator.MAX );
                // TODO: use y instead of this flag
            }
            else {
                _diceData.Add( _dice.offset_x );
                _diceData.Add( _dice.offset_y );
                _diceData.Add( _dice.rotated ? - _dice.width : _dice.width );
                _diceData.Add( _dice.height );
            }
            _diceData.Add( _dice.x );
            _diceData.Add( _dice.y );
        }
    }

    public exTextureInfo.Dice Current {
        get {
            exTextureInfo.Dice d = new exTextureInfo.Dice();
#if UNITY_EDITOR
            d.enumerator = this;
#endif
            if (diceData[dataIndex] == EMPTY) {
                d.sizeType = exTextureInfo.DiceType.Empty;
                return d;
            }
            if (diceData[dataIndex] >= 0) {
                d.sizeType = exTextureInfo.DiceType.Trimmed;
                d.offset_x = diceData[dataIndex];
                d.offset_y = diceData[dataIndex + 1];
                d.width = diceData[dataIndex + 2];
                d.height = diceData[dataIndex + 3];
                d.x = diceData[dataIndex + 4];
                d.y = diceData[dataIndex + 5];
                if (d.width < 0) {
                    d.rotated = true;
                    d.width = -d.width;
                }
            }
            else {
                exDebug.Assert(diceData[dataIndex] == MAX || diceData[dataIndex] == MAX_ROTATED);
                d.sizeType = exTextureInfo.DiceType.Max;
                d.x = diceData[dataIndex + 1];
                d.y = diceData[dataIndex + 2];
                d.width = diceUnitWidth;
                d.height = diceUnitHeight;
                d.rotated = (diceData[dataIndex] == MAX_ROTATED);
            }
            return d;
        }
    }
    
    public bool MoveNext () {
#if UNITY_EDITOR
        ++diceIndex;
#endif
        if (dataIndex == -1) {
            dataIndex = 2;  // skip width and height
            if (diceData == null) {
                return false;
            }
        }
        else if (diceData[dataIndex] >= 0) {
            dataIndex += 6;
        }
        else if (diceData[dataIndex] == MAX || diceData[dataIndex] == MAX_ROTATED) {
            dataIndex += 3;
        }
        else {
            exDebug.Assert (diceData[dataIndex] == EMPTY);
            dataIndex += 1;
        }
        return dataIndex < diceData.Count;
    }

    public void Dispose () {
        diceData = null;
    }

    object System.Collections.IEnumerator.Current {
        get { return Current; }
    }

    public void Reset () {
        dataIndex = -1;
#if UNITY_EDITOR
        diceIndex = -1;
#endif
    }
}
}

#if UNITY_EDITOR

///////////////////////////////////////////////////////////////////////////////
///
/// The texture-info helper for editor
///
///////////////////////////////////////////////////////////////////////////////

public partial class exTextureInfo : ScriptableObject {

    private int rawEditorDiceUnitWidth_ = -1;    ///< not committed value, used for editor
    public int rawEditorDiceUnitWidth {
        get {
            if (rawEditorDiceUnitWidth_ != -1) {
                return rawEditorDiceUnitWidth_;
            }
            return diceUnitWidth;
        }
        set {
            rawEditorDiceUnitWidth_ = Mathf.Max(value, 0);
        }
    }

    private int rawEditorDiceUnitHeight_ = -1;   ///< not committed value, used for editor
    public int rawEditorDiceUnitHeight {
        get {
            if (rawEditorDiceUnitHeight_ != -1) {
                return rawEditorDiceUnitHeight_;
            }
            return diceUnitHeight;
        }
        set {
            rawEditorDiceUnitHeight_ = Mathf.Max(value, 0);
        }
    }
    
    public int editorDiceUnitWidth {
        get {
            if (rawEditorDiceUnitWidth_ == 0) {
                return width;
            }
            return rawEditorDiceUnitWidth;
        }
    }
    public int editorDiceUnitHeight {
        get {
            if (rawEditorDiceUnitHeight_ == 0) {
                return height;
            }
            return rawEditorDiceUnitHeight;
        }
    }

    public int editorDiceXCount {
        get {
            if (editorDiceUnitWidth > 0 && width > 0) {
                return Mathf.CeilToInt((float)width / editorDiceUnitWidth);
            }
            else {
                return 1;
            }
        }
    }
    public int editorDiceYCount {
        get {
            if (editorDiceUnitHeight > 0 && height > 0) {
                return Mathf.CeilToInt((float)height / editorDiceUnitHeight);
            }
            else {
                return 1;
            }
        }
    }

    public bool shouldDiced {
        get { return rawEditorDiceUnitWidth > 0 || rawEditorDiceUnitHeight > 0; }
    }

    private Dice[] editorDiceDatas = null;  ///< not committed value, used for editor

    // ------------------------------------------------------------------ 
    /// Start commit dice data
    // ------------------------------------------------------------------ 

    public void CreateDiceData () {
        editorDiceDatas = new Dice[editorDiceXCount * editorDiceYCount];
    }

    // ------------------------------------------------------------------ 
    // dice index:
    // 8  9  10 11
    // 4  5  6  7 
    // 0  1  2  3  
    // NOTE: 这里的_diceData.sizeType无用，可以为任意值
    // ------------------------------------------------------------------ 

    public void SetDiceData (int _diceIndex, Dice _dice) {
        int editorDiceCount = editorDiceXCount * editorDiceYCount;
        if (editorDiceDatas == null) {
            editorDiceDatas = new Dice[editorDiceCount];
            // decompress data
            int diceID = 0;
            foreach (Dice dice in dices) {
                if (diceID >= editorDiceDatas.Length) {
                    break;
                }
                editorDiceDatas[diceID++] = dice;
            }
        }
        else if (editorDiceDatas.Length != editorDiceCount) {
            System.Array.Resize(ref editorDiceDatas, editorDiceCount);
        }
        editorDiceDatas[_diceIndex] = _dice;
    }

    // ------------------------------------------------------------------ 
    /// Save committed value
    // ------------------------------------------------------------------ 

    public void CommitDiceData () {
        if (rawEditorDiceUnitWidth_ == -1) {
            rawEditorDiceUnitWidth_ = diceUnitWidth;    // keep dice value
        }
        if (rawEditorDiceUnitHeight_ == -1) {
            rawEditorDiceUnitHeight_ = diceUnitHeight;  // keep dice value
        }
        diceData.Clear();
        if (shouldDiced == false) {
            return;
        }
        diceData.Add (editorDiceUnitWidth);
        diceData.Add (editorDiceUnitHeight);
        foreach (Dice dice in editorDiceDatas) {
           DiceEnumerator.AddDiceData(this, diceData, dice);
        }
        // TODO: trim last empty dice
    }
}

#endif