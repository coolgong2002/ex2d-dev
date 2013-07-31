// ======================================================================================
// File         : exBitmapFont.cs
// Author       : Wu Jie 
// Last Change  : 07/26/2013 | 17:18:41 PM | Friday,July
// Description  : 
// ======================================================================================

///////////////////////////////////////////////////////////////////////////////
// usings
///////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

///////////////////////////////////////////////////////////////////////////////
///
/// The texture-info asset
///
///////////////////////////////////////////////////////////////////////////////

public class exBitmapFont : ScriptableObject {

    ///////////////////////////////////////////////////////////////////////////////
    ///
    /// A structure to descrip the character in the bitmap font 
    ///
    ///////////////////////////////////////////////////////////////////////////////

    [System.Serializable]
    public class CharInfo {
        public int id = -1;                ///< the char value
        public int x = -1;                 ///< the x pos   // TODO: why not use float type
        public int y = -1;                 ///< the y pos
        public int width = -1;             ///< the width
        public int height = -1;            ///< the height
        public int xoffset = -1;           ///< the xoffset
        public int yoffset = -1;           ///< the yoffset
        public int xadvance = -1;          ///< the xadvance
        public bool rotated = false;

        public CharInfo () {}
        public CharInfo ( CharInfo _c ) {
            id = _c.id;
            x = _c.x;
            y = _c.y;
            width = _c.width;
            height = _c.height;
            xoffset = _c.xoffset;
            yoffset = _c.yoffset;
            xadvance = _c.xadvance;
            rotated = _c.rotated;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////
    ///
    /// A structure to descrip the kerning between two character in the bitmap font 
    ///
    ///////////////////////////////////////////////////////////////////////////////

    [System.Serializable]
    public class KerningInfo {
        public char first = '\x0';  ///< the first character 
        public char second = '\x0'; ///< the second character
        public int amount = -1; ///< the amount of kerning
    }

    ///////////////////////////////////////////////////////////////////////////////
    ///
    /// A structure to descrip the pair of char
    ///
    ///////////////////////////////////////////////////////////////////////////////

    public struct KerningTableKey { // TODO: benchmark with dict<int, dict<int, int>>
        
        // ------------------------------------------------------------------ 
        /// In Xamarin.iOS, if KerningTableKey is a type as dictionary keys, we should manually implement its IEqualityComparer,
        /// and provide an instance to the Dictionary<TKey, TValue>(IEqualityComparer<TKey>) constructor.
        /// See http://docs.xamarin.com/guides/ios/advanced_topics/limitations for more info.
        // ------------------------------------------------------------------ 
        
        public class Comparer : IEqualityComparer<KerningTableKey> {
            static Comparer instance_;
            public static Comparer instance {
                get {
                    if (instance_ == null) {
                        instance_ = new Comparer();
                    }
                    return instance_;
                }
            }
            public bool Equals (KerningTableKey _lhs, KerningTableKey _rhs) {
                return _lhs.first == _rhs.first && _lhs.second == _rhs.second;
            }
            public int GetHashCode(KerningTableKey _obj) {
                return ((int)_obj.first << 16) ^ _obj.second;
            }
        }
        
        public char first;
        public char second;

        public KerningTableKey (char _first, char _second) {
            first = _first;
            second = _second;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////
    // serialized fileds
    ///////////////////////////////////////////////////////////////////////////////

    public string rawFontGUID = "";
    /// <summary>
    /// The texture.
    /// </summary>
    public Texture2D texture; ///< the atlas or raw texture

    /// <summary>
    /// The char infos.
    /// </summary>
    public List<CharInfo> charInfos = new List<CharInfo>(); ///< the list of the character information
    /// <summary>
    /// The kernings.
    /// </summary>
    public List<KerningInfo> kernings = new List<KerningInfo>(); ///< the list of the kerning information 

    /// <summary>
    /// The height of the line.
    /// </summary>
    public int lineHeight; ///< the space of the line
    /// <summary>
    /// The size.
    /// </summary>
    public int size;       ///< the size in pixel of the font 

    ///////////////////////////////////////////////////////////////////////////////
    // internal fileds
    ///////////////////////////////////////////////////////////////////////////////

    protected Dictionary<int,CharInfo> charInfoTable = null;
    protected Dictionary<KerningTableKey,int> kerningTable = null;

    ///////////////////////////////////////////////////////////////////////////////
    // functions
    ///////////////////////////////////////////////////////////////////////////////

    public void Reset () {
        rawFontGUID = "";
        texture = null;

        charInfos.Clear();
        kernings.Clear();

        lineHeight = 0;
        size = 0;

        charInfoTable = null;
        kerningTable = null;
    }

    // ------------------------------------------------------------------ 
    /// Rebuild the table to store key exBitmapFont.CharInfo.id to value exBitmapFont.CharInfo
    // ------------------------------------------------------------------ 

    public void RebuildCharInfoTable () {
        if ( charInfoTable == null ) {
            charInfoTable = new Dictionary<int,CharInfo>(charInfos.Count);
        }
        charInfoTable.Clear();
        for ( int i = 0; i < charInfos.Count; ++i ) {
            CharInfo c = charInfos[i];
            charInfoTable[c.id] = c;
        }
    }

    // ------------------------------------------------------------------ 
    /// \param _id the look up key 
    /// \return the expect character info
    /// Get the character information by exBitmapFont.CharInfo.id
    // ------------------------------------------------------------------ 

    public CharInfo GetCharInfo ( char _symbol ) {
        // create and build idToCharInfo table if null
        if ( charInfoTable == null ) {
            RebuildCharInfoTable ();
        }

        CharInfo charInfo;
        if ( charInfoTable.TryGetValue (_symbol, out charInfo) )
            return charInfo;
        return null;
    }
    
    // ------------------------------------------------------------------ 
    /// Rebuild the kerningTable to store key <first char, second char> to value kerning amount
    // ------------------------------------------------------------------ 

    public void RebuildKerningTable () {
        if ( kerningTable == null ) {
            kerningTable = new Dictionary<KerningTableKey,int> (kernings.Count, KerningTableKey.Comparer.instance);
        }
        kerningTable.Clear();
        for ( int i = 0; i < kernings.Count; ++i ) {
            KerningInfo k = kernings[i];
            kerningTable[new KerningTableKey(k.first, k.second)] = k.amount;
        }
    }

    // ------------------------------------------------------------------ 
    /// \param _first the first character
    /// \param _second the second character
    /// \return the kerning amount
    /// Get the kerning amount between first and sceond character
    // ------------------------------------------------------------------ 

    public int GetKerning ( char _first, char _second ) {
        if ( kerningTable == null ) {
            RebuildKerningTable ();
        }

        int amount;
        if ( kerningTable.TryGetValue (new KerningTableKey (_first, _second), out amount) ) {
            return amount;
        }
        return 0;
    }
}
