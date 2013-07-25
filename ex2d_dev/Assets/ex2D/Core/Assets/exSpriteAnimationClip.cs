﻿// ======================================================================================
// File         : exSpriteAnimationClip.cs
// Author       : Wu Jie 
// Last Change  : 07/17/2013 | 10:19:03 AM | Wednesday,July
// Description  : 
// ======================================================================================

#define DUPLICATE_WHEN_PINGPONE

///////////////////////////////////////////////////////////////////////////////
// usings
///////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

///////////////////////////////////////////////////////////////////////////////
///
/// The sprite animation clip asset used in exSpriteAnimation component. 
///
///////////////////////////////////////////////////////////////////////////////

public class exSpriteAnimationClip : ScriptableObject {

    // ------------------------------------------------------------------ 
    /// The action type used when animation stpped
    // ------------------------------------------------------------------ 

    public enum StopAction {
        DoNothing,      ///< do nothing
        DefaultSprite,  ///< set to default sprite when the sprite animation stopped
        Hide,           ///< hide the sprite when the sprite animation stopped
        Destroy         ///< destroy the GameObject the sprite belongs to when the sprite animation stopped
    }

    // ------------------------------------------------------------------ 
    /// The structure to descrip a frame in the sprite animation clip
    // ------------------------------------------------------------------ 

    [System.Serializable]
    public class FrameInfo {
        public exTextureInfo textureInfo; ///< the texture info used in this frame
        public int frames = 1;     ///< frame count

        public FrameInfo (exTextureInfo _textureInfo, int _frames) {
            textureInfo = _textureInfo;
            frames = _frames;
        }
    }

    // ------------------------------------------------------------------ 
    /// The structure to descrip an event in the sprite animation clip
    // ------------------------------------------------------------------ 

    [System.Serializable]
    public class EventInfo {

        public class SearchComparer : IComparer<EventInfo> {
            private static SearchComparer instance_;
            private static int frame;
            public static int BinarySearch (List<EventInfo> _list, int _frame) {
                frame = _frame;
                if (instance_ == null) {
                    instance_ = new SearchComparer();
                }
                return _list.BinarySearch(null, instance_);
            }
            public int Compare (EventInfo _x, EventInfo _y) {
                if (_x == null && _y == null) {
                    exDebug.Assert(false, "Failed to trigger current event because event list contains null event.", false);
                    return 0;
                }
                if (_x != null) {
                    if (_x.frame > frame)
                        return 1;
                    else if (_x.frame < frame)
                        return -1;
                    else
                        return 0;
                }
                else {
                    if (frame > _y.frame)
                        return 1;
                    else if (frame < _y.frame)
                        return -1;
                    else
                        return 0;
                }
            }
        }

        // ------------------------------------------------------------------ 
        /// the type of the parameter
        // ------------------------------------------------------------------ 

        public enum ParamType {
            None,   ///< none
            String, ///< string
            Float,  ///< float
            Int,    ///< int
            Bool,   ///< bool
            Object  ///< object
        }

        public int frame = 0; ///< the frame trigger the event
        public string methodName = ""; ///< the name of method to invoke 
        public ParamType paramType = ParamType.None; ///< the first parameter type 
        public string stringParam = ""; ///< the value of the string parameter
        public float floatParam = 0.0f; ///< the value of the float parameter
        public int intParam = -1; ///< the value of the int parameter
        public bool boolParam { ///< the value of the boolean parameter
            get {
                return intParam != 0;
            }
            set {
                intParam = value ? 1 : 0;
            }
        }
        public Object objectParam = null; ///< the value of the object parameter
        public SendMessageOptions msgOptions = SendMessageOptions.RequireReceiver; ///< the SendMessage option

        // ------------------------------------------------------------------ 
        /// Calls the method named methodName on every Component target game object.
        // ------------------------------------------------------------------ 

        public void Trigger (Component _target) {
            if (methodName == "")
                return;
            switch (paramType) {
            case ParamType.None:
                _target.SendMessage(methodName, msgOptions);
                break;
            case ParamType.String:
                _target.SendMessage(methodName, stringParam, msgOptions);
                break;
            case ParamType.Float:
                _target.SendMessage(methodName, floatParam, msgOptions);
                break;
            case ParamType.Int:
                _target.SendMessage(methodName, intParam, msgOptions);
                break;
            case ParamType.Bool:
                _target.SendMessage(methodName, boolParam, msgOptions);
                break;
            case ParamType.Object:
                _target.SendMessage(methodName, objectParam, msgOptions);
                break;
            }
        }
    }

    public WrapMode wrapMode = WrapMode.Once; ///< default wrap mode
    public StopAction stopAction = StopAction.DoNothing; ///< the default type of action used when the animation stopped 

    // ------------------------------------------------------------------ 
    [SerializeField] protected float frameRate_ = 60.0f;
    /// the sample rate used in this animation clip
    // ------------------------------------------------------------------ 

    public float frameRate {
        get { return frameRate_; }
        set {
            if ( value != frameRate_ ) {
                frameRate_ = Mathf.RoundToInt(Mathf.Max(value,1.0f)); 
            }
        }
    }

    public List<FrameInfo> frameInfos = new List<FrameInfo>(); ///< the list of frame info 
    public List<EventInfo> eventInfos = new List<EventInfo>(); ///< the list of event info
    public float speed = 1.0f; ///< the default speed of the animation clip

    ///////////////////////////////////////////////////////////////////////////////
    // functions
    ///////////////////////////////////////////////////////////////////////////////

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public int GetTotalFrames () {
        int frames = 0;
        for ( int i = 0; i < frameInfos.Count; ++i ) {
            frames += frameInfos[i].frames;
        }
        return frames;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public float GetLength () {
        return (float)GetTotalFrames() / frameRate_;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void AddFrame ( exTextureInfo _info, int _frames = 1 ) {
        InsertFrameInfo ( frameInfos.Count, new FrameInfo(_info,_frames) );
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void AddFrameAt ( int _idx, exTextureInfo _info, int _frames = 1 ) {
        InsertFrameInfo ( _idx, new FrameInfo(_info,_frames) );
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void RemoveFrame ( FrameInfo _frameInfo ) {
        frameInfos.Remove (_frameInfo);
        // TODO: should move all events behind this frame
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void InsertFrameInfo ( int _idx, FrameInfo _frameInfo ) {
        frameInfos.Insert (_idx,_frameInfo);
        // TODO: should move all events behind this frame
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void AddEmptyEvent ( int _frame ) {
        EventInfo ei = new EventInfo();
        ei.frame = _frame;
        AddEvent(ei);
    }

    // ------------------------------------------------------------------ 
    /// \param _e the event information wants to add
    /// add an event information
    // ------------------------------------------------------------------ 

    public void AddEvent ( EventInfo _eventInfo ) {
        if ( eventInfos.Count == 0 ) {
            eventInfos.Insert( 0, _eventInfo );
        }
        else if ( eventInfos.Count == 1 ) {
            if ( _eventInfo.frame >= eventInfos[0].frame )
                eventInfos.Insert( 1, _eventInfo );
            else
                eventInfos.Insert( 0, _eventInfo );
        }
        else {
            bool inserted = false;
            EventInfo lastEventInfo = eventInfos[0];

            for ( int i = 1; i < eventInfos.Count; ++i ) {
                EventInfo ei = eventInfos[i];
                if ( _eventInfo.frame >= lastEventInfo.frame && _eventInfo.frame < ei.frame ) {
                    eventInfos.Insert( i, _eventInfo );
                    inserted = true;
                    break;
                }

                lastEventInfo = ei; 
            }

            if ( inserted == false ) {
                eventInfos.Insert( eventInfos.Count, _eventInfo );
            }
        }
    }

    // ------------------------------------------------------------------ 
    /// \param _e the event information wants to remove
    /// remove an event information
    // ------------------------------------------------------------------ 

    public void RemoveEvent ( EventInfo _eventInfo ) {
        eventInfos.Remove( _eventInfo );
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void SortEvents () {
        eventInfos.Sort ( delegate ( EventInfo _x, EventInfo _y ) {
                            if ( _x.frame > _y.frame )
                                return 1;
                            else if ( _x.frame == _y.frame )
                                return 0;
                            else
                                return -1;
                          } );
    }

    // ------------------------------------------------------------------ 
    /// \param _target send event messages to the target
    /// \param _start the unwrapped start frame index
    /// \param _end the unwrapped end frame index
    /// \param _wrapMode the wrap mode
    /// \param _totalFrame total frame count cached by caller
    /// Trigger events locate between the start and end frame
    // ------------------------------------------------------------------ 

    public void TriggerEvents (Component _target, int _start, float _end, WrapMode _wrapMode, int _totalFrame = -1) {
        exDebug.Assert(_totalFrame == -1 || _totalFrame == GetTotalFrames());
        
        if (eventInfos.Count == 0)
            return;

        //get frame count
        if (_totalFrame == -1) {
            _totalFrame = GetTotalFrames();
        }

        for (int i = _start; i <= _end; ++i) {
            if (_totalFrame == 0) {
                TriggerEvents(_target, 0, false);
                continue;
            }
            int wrappedIndex;
            bool reversed = false;
            if (wrapMode == WrapMode.PingPong) {
#if DUPLICATE_WHEN_PINGPONE
                //wrappedIndex = exMath.Wrap(i, _totalFrame, _wrapMode);
                int cnt = i / _totalFrame;
                wrappedIndex = i % _totalFrame;
                reversed = (cnt % 2 == 1);
                if (reversed) {
                    wrappedIndex = _totalFrame - wrappedIndex;
                }
#else
                int cnt = (i - 1) / (_totalFrame - 1);
                wrappedIndex = (i - 1) % (_totalFrame - 1) + 1;
                bool skippedEndsEvent = (i > 1 && wrappedIndex == 1);
                reversed = (cnt % 2 == 1);
                if (reversed) {
                    if (skippedEndsEvent) {
                        TriggerEvents(_target, _totalFrame, true);
                    }
                    wrappedIndex = _totalFrame - wrappedIndex;
                }
                else {
                    if (skippedEndsEvent) {
                        TriggerEvents(_target, 0, false);
                    }
                }
#endif
            }
            else if (wrapMode == WrapMode.Loop) {
                wrappedIndex = exMath.Wrap(i, _totalFrame - 1, _wrapMode);
                bool skippedFinalEvent = (i > 0 && wrappedIndex == 0);
                if (skippedFinalEvent) {
                    TriggerEvents(_target, _totalFrame, false);
                }
            }
            else {
                exDebug.Assert(i <= _totalFrame);
                wrappedIndex = i;
            }
            TriggerEvents(_target, wrappedIndex, reversed);
        }
    }

    // ------------------------------------------------------------------ 
    /// \param _target send event messages to the target
    /// \param _wrappedFrame the wrapped frame to trigger
    /// \param _reversed reversed trigger order between same frame events
    /// Trigger all events at the frame
    // ------------------------------------------------------------------ 

    private void TriggerEvents (Component _target, int _wrappedIndex, bool _reversed) {
        //Debug.Log(string.Format("[TriggerEvents|exSpriteAnimationClip] _wrappedIndex: {0} " + _reversed, _wrappedIndex));
        if (eventInfos.Count == 0) {
            return;
        }
        if (_reversed) {
            int searchStart = EventInfo.SearchComparer.BinarySearch(eventInfos, _wrappedIndex + 1);
            if (searchStart < 0) {
                searchStart = ~searchStart;
                if (searchStart >= eventInfos.Count) {
                    searchStart = eventInfos.Count - 1;
                }
            }
            for (int i = searchStart; i >= 0; --i) {
                EventInfo eventInfo = eventInfos[i];
                if (eventInfo.frame == _wrappedIndex) {
                    eventInfo.Trigger(_target);
                }
                else if (eventInfo.frame < _wrappedIndex) {
                    break;
                }
            }
        }
        else {
            int searchStart = EventInfo.SearchComparer.BinarySearch(eventInfos, _wrappedIndex - 1);
            if (searchStart < 0) {
                searchStart = ~searchStart;
                if (searchStart >= eventInfos.Count) {
                    return;
                }
            }
            for (int i = searchStart; i < eventInfos.Count; ++i) {
                EventInfo eventInfo = eventInfos[i];
                if (eventInfo.frame == _wrappedIndex) {
                    eventInfo.Trigger(_target);
                }
                else if (eventInfo.frame > _wrappedIndex) {
                    break;
                }
            }
        }
    }
}
