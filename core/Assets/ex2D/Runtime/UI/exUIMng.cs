// ======================================================================================
// File         : exUIMng.cs
// Author       : Wu Jie 
// Last Change  : 10/05/2013 | 10:58:01 AM | Saturday,October
// Description  : 
// ======================================================================================

///////////////////////////////////////////////////////////////////////////////
// usings
///////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

///////////////////////////////////////////////////////////////////////////////
// defines
///////////////////////////////////////////////////////////////////////////////

// ------------------------------------------------------------------ 
// Desc: 
// ------------------------------------------------------------------ 

public class RaycastSorter : IComparer {
    int IComparer.Compare ( object _a, object _b ) {
        if ( !(_a is RaycastHit) || !(_b is RaycastHit) ) 
            return 0;

        RaycastHit raycastHitA = (RaycastHit)_a;
        RaycastHit raycastHitB = (RaycastHit)_b;

        return (int)Mathf.Sign(raycastHitA.distance - raycastHitB.distance);
    }
}

// ------------------------------------------------------------------ 
// Desc: 
// ------------------------------------------------------------------ 

public class ControlSorter: IComparer<exUIControl> {
    public int Compare( exUIControl _a, exUIControl _b ) {
        exUIControl parent = null;
        int level_a = 0;
        int level_b = 0;

        // a level
        parent = _a.parent;
        while ( parent ) {
            ++level_a; 
            parent = parent.parent;
        }

        // b level
        parent = _b.parent;
        while ( parent ) {
            ++level_b; 
            parent = parent.parent;
        }

        return level_a - level_b;
    }
}

// ------------------------------------------------------------------ 
// Desc: 
// ------------------------------------------------------------------ 

public class ControlSorterByZ: IComparer<exUIControl> {
    public int Compare( exUIControl _a, exUIControl _b ) {
        int r = Mathf.CeilToInt(_a.transform.position.z - _b.transform.position.z);
        if ( r != 0 )
            return r;
        return _a.GetInstanceID() - _b.GetInstanceID();
    }
}

// ------------------------------------------------------------------ 
// Desc: 
// ------------------------------------------------------------------ 

public class exUIEvent {

    ///////////////////////////////////////////////////////////////////////////////
    // structures
    ///////////////////////////////////////////////////////////////////////////////

    public enum Type {
        Unknown = -1,
        MouseDown = 0,
        MouseUp,
        MouseMove,
        MouseEnter,
        MouseExit,
        TouchDown,
        TouchUp,
        TouchMove, 
        TouchEnter, 
        TouchExit, 
        KeyDown,
        KeyUp,
        // GamePadButtonDown,
        // GamePadButtonUp,
    }

    public enum Category {
        None = 0,
        Mouse,
        Keyboard,
        GamePad,
        Touch
    }

	[System.FlagsAttribute]
    public enum MouseButtonFlags {
        None    = 0,
        Left    = 1,
        Middle  = 2,
        Right   = 4,
    }

    ///////////////////////////////////////////////////////////////////////////////
    // properties
    ///////////////////////////////////////////////////////////////////////////////

    public Category category = Category.None; // the event category
    public Type type = Type.Unknown;
    public Vector2 position = Vector2.zero;
    public Vector2 delta = Vector2.zero;
    public MouseButtonFlags buttons = MouseButtonFlags.None;
    public int touchID = -1;
    public KeyCode keyCode; // TODO:
}

// ------------------------------------------------------------------ 
// Desc: 
// ------------------------------------------------------------------ 

[ExecuteInEditMode]
public class exUIMng : MonoBehaviour {

    protected static exUIMng inst_ = null; 
    public static exUIMng inst {
        get {
            if ( inst_ == null ) {
                inst_ = FindObjectOfType ( typeof(exUIMng) ) as exUIMng;
            }
            return inst_;
        }
    }

    // static  RaycastSorter raycastSorter = new RaycastSorter();
    static ControlSorter controlSorter = new ControlSorter();
    static ControlSorterByZ controlSorterByZ = new ControlSorterByZ();

    ///////////////////////////////////////////////////////////////////////////////
    // structures
    ///////////////////////////////////////////////////////////////////////////////

    public class EventInfo { 
        public exUIControl primaryControl = null;
        public exUIEvent uiEvent = null;
    }

    //
    [System.Serializable]
    public class TouchState {
        public bool active = false;
        public int touchID = -1;
        public exUIControl hotControl = null;
        public exUIControl keyboardControl = null;
    }

    //
    [System.Serializable]
    public class MouseState {
        public Vector2 currentPos = Vector2.zero;
        public exUIEvent.MouseButtonFlags currentButtons = exUIEvent.MouseButtonFlags.None;
        public exUIControl hotControl = null;
        public exUIControl keyboardControl = null;
    }

    ///////////////////////////////////////////////////////////////////////////////
    // serializable 
    ///////////////////////////////////////////////////////////////////////////////

    public bool useRayCast = false; /// if your UI control is in 3D space, turn this on.
    public bool showDebugInfo = false;
    public bool showDebugInfoInGameView = false;

    ///////////////////////////////////////////////////////////////////////////////
    // non-serialized
    ///////////////////////////////////////////////////////////////////////////////

    bool initialized = false;
    List<exUIControl> rootControls = new List<exUIControl>();

    // internal ui status
    MouseState mouseState = new MouseState();
    TouchState[] touchStates = new TouchState[10];

    //
    List<EventInfo> eventInfos = new List<EventInfo>();

    ///////////////////////////////////////////////////////////////////////////////
    // functions
    ///////////////////////////////////////////////////////////////////////////////

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void Awake () {
        Init ();
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void Start () {
#if UNITY_IPHONE
        if ( Application.isEditor == false ) {
        } 
        else {
#endif
            mouseState.currentPos = Input.mousePosition;
#if UNITY_IPHONE
        }
#endif
    }
	
    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

	void Update () {
        HandleEvents ();
        DispatchEvents ();
	}

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void OnGUI () {
        if ( showDebugInfoInGameView ) {
            ShowDebugInfo ( new Rect(10, 10, 300, 300) );
        }
    }

    ///////////////////////////////////////////////////////////////////////////////
    //
    ///////////////////////////////////////////////////////////////////////////////

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void Init () {
        if ( initialized )
            return;

        //
        if ( camera == null ) {
            Debug.LogError ( "The exUIMng should attach to a camera" );
            return;
        }

        //
        for ( int i = 0; i < 10; ++i ) {
            touchStates[i] = new TouchState();
        }

        // recursively add ui-tree
        exUIControl[] controls = FindObjectsOfType(typeof(exUIControl)) as exUIControl[];
        for ( int i = 0; i < controls.Length; ++i ) {
            exUIControl ctrl = controls[i];
            exUIControl parent_ctrl = ctrl.FindParent();
            if ( parent_ctrl == null ) {
                exUIControl.FindAndAddChild (ctrl);

                //
                if ( rootControls.IndexOf(ctrl) == -1 ) {
                    rootControls.Add(ctrl);
                }
            }
        }

        //
        mouseState.currentPos = Vector2.zero;
        mouseState.currentButtons = exUIEvent.MouseButtonFlags.None;
        mouseState.hotControl = null;
        mouseState.keyboardControl = null;

        //
        initialized = true;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void AddRootControl ( exUIControl _ctrl ) {
        if ( rootControls.IndexOf(_ctrl) == -1 )
            rootControls.Add(_ctrl);
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void HandleEvents () {
        HandlePointerEvents ();
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void DispatchEvents () {
        // for ( int i = 0; i < eventInfos.Count; ++i ) {
        //     EventInfo info = eventInfos[i];
        //     bool used = info.primaryControl.OnEvent(info.uiEvent);
        //     exUIControl uiParent = info.primaryControl;
        //     while ( used == false ) {
        //         uiParent = uiParent.parent;
        //         if ( uiParent == null )
        //             break;
        //         used = uiParent.OnEvent(info.uiEvent);
        //     }
        // }
        // eventInfos.Clear();
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void HandlePointerEvents () {
#if UNITY_IPHONE
        if ( Application.isEditor == false ) {
            ProcessTouches();
        } else {
#endif
            ProcessMouse();
#if UNITY_IPHONE
        }
#endif
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void ProcessTouches () {
        for ( int i = 0; i < Input.touches.Length; ++i ) {
            Touch touch = Input.touches[i];
            if ( touch.fingerId >= 10 )
                continue;

            TouchState touchState = null;
            exUIControl hotControl = PickControl(touch.position);
            
            //
            if ( touch.phase == TouchPhase.Began ) {
                if ( hotControl != null ) {
                    exUIEvent e = new exUIEvent(); 
                    e.category = exUIEvent.Category.Touch;
                    e.type =  exUIEvent.Type.TouchDown;
                    e.position = touch.position;
                    e.delta = touch.deltaPosition;
                    e.touchID = touch.fingerId;

                    EventInfo info = new EventInfo();
                    info.primaryControl = hotControl;
                    info.uiEvent = e;
                    eventInfos.Add(info);
                }

                // NOTE: it must be null
                SetTouchFocus ( touch.fingerId, null );
                touchStates[touch.fingerId].hotControl = hotControl;
            }
            else {
                // find the touch state
                touchState = touchStates[touch.fingerId];

                // set the last and current hot control 
                exUIControl keyboardControl = null;
                exUIControl lastCtrl = null;
                if ( touchState != null ) {
                    lastCtrl = touchState.hotControl;
                    touchState.hotControl = hotControl;
                    keyboardControl = touchState.keyboardControl;
                }

                if ( touch.phase == TouchPhase.Ended ) {
                    if ( touchState != null ) {
                        if ( keyboardControl != null ) {
                            exUIEvent e = new exUIEvent(); 
                            e.category = exUIEvent.Category.Touch;
                            e.type =  exUIEvent.Type.TouchUp;
                            e.position = touch.position;
                            e.delta = touch.deltaPosition;
                            e.touchID = touch.fingerId;

                            EventInfo info = new EventInfo();
                            info.primaryControl = keyboardControl;
                            info.uiEvent = e;
                            eventInfos.Add(info);
                        }
                    }
                }
                else if ( touch.phase == TouchPhase.Canceled ) {
                    if ( touchState != null )
                        SetTouchFocus ( touch.fingerId, null );
                }
                else if ( touch.phase == TouchPhase.Moved ) {
                    // process hover event
                    if ( lastCtrl != hotControl ) {
                        // add hover-in event
                        if ( hotControl != null ) {
                            exUIEvent e = new exUIEvent(); 
                            e.category = exUIEvent.Category.Touch;
                            e.type =  exUIEvent.Type.TouchEnter;
                            e.position = touch.position;
                            e.delta = touch.deltaPosition;
                            e.touchID = touch.fingerId;

                            EventInfo info = new EventInfo();
                            info.primaryControl = hotControl;
                            info.uiEvent = e;
                            eventInfos.Add(info);
                        }

                        // add hover-out event
                        if ( lastCtrl != null ) {
                            exUIEvent e = new exUIEvent(); 
                            e.category = exUIEvent.Category.Touch;
                            e.type =  exUIEvent.Type.TouchExit;
                            e.position = touch.position;
                            e.delta = touch.deltaPosition;
                            e.touchID = touch.fingerId;

                            EventInfo info = new EventInfo();
                            info.primaryControl = lastCtrl;
                            info.uiEvent = e;
                            eventInfos.Add(info);
                        }
                    }

                    //
                    if ( hotControl != null || keyboardControl != null ) {
                        exUIEvent e = new exUIEvent(); 
                        e.category = exUIEvent.Category.Touch;
                        e.type =  exUIEvent.Type.TouchMove;
                        e.position = touch.position;
                        e.delta = touch.deltaPosition;
                        e.touchID = touch.fingerId;

                        EventInfo info = new EventInfo();
                        info.primaryControl = (keyboardControl != null) ? keyboardControl : hotControl;
                        info.uiEvent = e;
                        eventInfos.Add(info);
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    void ProcessMouse () {
        // get current position and delta pos
        Vector2 lastPointerPos = mouseState.currentPos;
        mouseState.currentPos = Input.mousePosition;
        Vector2 deltaPos = mouseState.currentPos - lastPointerPos;

        // get current mouse button
        exUIEvent.MouseButtonFlags lastButtons = mouseState.currentButtons;
        exUIEvent.MouseButtonFlags buttonDown = exUIEvent.MouseButtonFlags.None;
        exUIEvent.MouseButtonFlags buttonUp = exUIEvent.MouseButtonFlags.None;

        // handle pressed
        mouseState.currentButtons = exUIEvent.MouseButtonFlags.None;
        if ( Input.anyKey ) {
            if ( Input.GetMouseButton(0) )
                mouseState.currentButtons |= exUIEvent.MouseButtonFlags.Left;
            if ( Input.GetMouseButton(1) )
                mouseState.currentButtons |= exUIEvent.MouseButtonFlags.Right;
            if ( Input.GetMouseButton(2) )
                mouseState.currentButtons |= exUIEvent.MouseButtonFlags.Middle;
        }

        // handle press
        if ( Input.anyKeyDown ) {
            if ( Input.GetMouseButtonDown(0) )
                buttonDown = exUIEvent.MouseButtonFlags.Left;
            else if ( Input.GetMouseButtonDown(1) )
                buttonDown = exUIEvent.MouseButtonFlags.Right;
            else if ( Input.GetMouseButtonDown(2) )
                buttonDown = exUIEvent.MouseButtonFlags.Middle;
        }

        // handle release
        if ( lastButtons != mouseState.currentButtons ) {
            if ( Input.GetMouseButtonUp(0) )
                buttonUp = exUIEvent.MouseButtonFlags.Left;
            else if ( Input.GetMouseButtonUp(1) )
                buttonUp = exUIEvent.MouseButtonFlags.Right;
            else if ( Input.GetMouseButtonUp(2) )
                buttonUp = exUIEvent.MouseButtonFlags.Middle;
        }
        
        // get hot control
        exUIControl lastCtrl = mouseState.hotControl;
        exUIControl curCtrl = PickControl(mouseState.currentPos);
        mouseState.hotControl = curCtrl;

        // DEBUG { 
        // if ( curCtrl != null )
        //     Debug.Log("curCtrl = " + curCtrl.name);
        // } DEBUG end 

        // process hover event
        if ( lastCtrl != curCtrl ) {
            // on hover out
            if ( lastCtrl != null ) {
                lastCtrl.Send_OnHoverOut();
            }

            // on hover in
            if ( curCtrl != null ) {
                curCtrl.Send_OnHoverIn();
            }
        }

        // // add pointer-move event
        // if ( (mouseState.hotControl != null || mouseState.keyboardControl != null) && deltaPos != Vector2.zero ) {
        //     exUIEvent e = new exUIEvent(); 
        //     e.category = exUIEvent.Category.Mouse;
        //     e.type =  exUIEvent.Type.MouseMove;
        //     e.position = mouseState.currentPos;
        //     e.delta = deltaPos;
        //     e.buttons = mouseState.currentButtons;

        //     EventInfo info = new EventInfo();
        //     info.primaryControl = (mouseState.keyboardControl != null) ? mouseState.keyboardControl : mouseState.hotControl;
        //     info.uiEvent = e;
        //     eventInfos.Add(info);
        // }

        // // add pointer-press event
        // if ( mouseState.hotControl != null && buttonDown != exUIEvent.MouseButtonFlags.None ) {
        //     exUIEvent e = new exUIEvent(); 
        //     e.category = exUIEvent.Category.Mouse;
        //     e.type =  exUIEvent.Type.MouseDown;
        //     e.position = mouseState.currentPos;
        //     e.delta = deltaPos;
        //     e.buttons = buttonDown;

        //     EventInfo info = new EventInfo();
        //     info.primaryControl = mouseState.hotControl;
        //     info.uiEvent = e;
        //     eventInfos.Add(info);
        // }

        // // add pointer-release event
        // if ( mouseState.keyboardControl != null && buttonUp != exUIEvent.MouseButtonFlags.None ) {
        //     exUIEvent e = new exUIEvent(); 
        //     e.category = exUIEvent.Category.Mouse;
        //     e.type =  exUIEvent.Type.MouseUp;
        //     e.position = mouseState.currentPos;
        //     e.delta = deltaPos;
        //     e.buttons = buttonUp;

        //     EventInfo info = new EventInfo();
        //     info.primaryControl = mouseState.keyboardControl;
        //     info.uiEvent = e;
        //     eventInfos.Add(info);
        // }
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    exUIControl PickControl ( Vector2 _screenPos ) {
        if ( useRayCast ) {
            Ray ray = camera.ScreenPointToRay ( _screenPos );
            ray.origin = new Vector3 ( ray.origin.x, ray.origin.y, camera.transform.position.z );
            RaycastHit[] hits = Physics.RaycastAll(ray);
            // DISABLE { 
            // System.Array.Sort(hits, raycastSorter);
            // if ( hits.Length > 0 ) {
            //     for ( int i = 0; i < hits.Length; ++i ) {
            //         RaycastHit hit = hits[i];
            //         GameObject go = hit.collider.gameObject;
            //         exUIControl ctrl = go.GetComponent<exUIControl>();
            //         if ( ctrl && ctrl.enabled ) {
            //             return ctrl;
            //         }
            //     }
            // }
            // return null;
            // } DISABLE end 

            List<exUIControl> controls = new List<exUIControl>();
            for ( int i = 0; i < hits.Length; ++i ) {
                RaycastHit hit = hits[i];
                GameObject go = hit.collider.gameObject;
                exUIControl ctrl = go.GetComponent<exUIControl>();
                if ( ctrl && ctrl.isActive ) {
                    controls.Add(ctrl);
                }
            }
            if ( controls.Count > 0 ) {
                controls.Sort(controlSorter);
                return controls[controls.Count-1]; 
            }
            return null;
        }
        else {
            Vector3 worldPointerPos = camera.ScreenToWorldPoint ( _screenPos );
            rootControls.Sort(controlSorterByZ);
            for ( int i = 0; i < rootControls.Count; ++i ) {
                exUIControl ctrl = rootControls[i];
                exUIControl resultCtrl = RecursivelyGetUIControl ( ctrl, worldPointerPos );
                if ( resultCtrl != null )
                    return resultCtrl;
            }
            return null;
        }
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    exUIControl RecursivelyGetUIControl ( exUIControl _ctrl, Vector2 _worldPos ) {
        if ( _ctrl.gameObject.activeInHierarchy == false || _ctrl.enabled == false )
            return null;

        //
        Vector2 localPos = new Vector2( _worldPos.x - _ctrl.transform.position.x, 
                                        _worldPos.y - _ctrl.transform.position.y );
        localPos.y = -localPos.y;

        Rect boundingRect = _ctrl.GetLocalAABoundingRect();
        if ( boundingRect.Contains(localPos) ) {
            for ( int i = 0; i < _ctrl.children.Count; ++i ) {
                exUIControl childCtrl = _ctrl.children[i];
                exUIControl resultCtrl = RecursivelyGetUIControl ( childCtrl, _worldPos );
                if ( resultCtrl != null )
                    return resultCtrl;
            }
            return _ctrl;
        }

        return null;
    } 

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public exUIControl GetTouchFocus ( int _touchID ) { 
        return touchStates[_touchID].keyboardControl;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void SetTouchFocus ( int _touchID, exUIControl _ctrl ) { 
        touchStates[_touchID].keyboardControl = _ctrl;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void SetTouchFocus_NoticeUnfocusTarget ( int _touchID, exUIControl _ctrl ) { 
        if ( touchStates[_touchID].keyboardControl != null &&
             _ctrl != touchStates[_touchID].keyboardControl ) {

            // add hover-out event
            exUIEvent e = new exUIEvent(); 
            e.category = exUIEvent.Category.Touch;
            e.type =  exUIEvent.Type.TouchExit;
            // e.position = touch.position;
            // e.delta = touch.deltaPosition;
            e.touchID = _touchID;

            EventInfo info = new EventInfo();
            info.primaryControl = touchStates[_touchID].keyboardControl;
            info.uiEvent = e;
            eventInfos.Add(info);
        }

        touchStates[_touchID].keyboardControl = _ctrl;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public exUIControl GetMouseFocus () {
        return mouseState.keyboardControl;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void SetMouseFocus ( exUIControl _ctrl ) {
        mouseState.keyboardControl = _ctrl;
    }

    // ------------------------------------------------------------------ 
    // Desc: 
    // ------------------------------------------------------------------ 

    public void SetMouseFocus_NoticeUnfocusTarget ( exUIControl _ctrl ) {
        if ( mouseState.keyboardControl != null &&
             _ctrl != mouseState.keyboardControl ) {

            // add hover-out event
            exUIEvent e = new exUIEvent(); 
            e.category = exUIEvent.Category.Mouse;
            e.type =  exUIEvent.Type.MouseExit;
            // e.position = mouseState.currentPos;
            // e.delta = deltaPos;
            // e.buttons = mouseState.currentButtons;

            EventInfo info = new EventInfo();
            info.primaryControl = mouseState.keyboardControl;
            info.uiEvent = e;
            eventInfos.Add(info);
        }

        mouseState.keyboardControl = _ctrl;
    }

    ///////////////////////////////////////////////////////////////////////////////
    // Debug
    ///////////////////////////////////////////////////////////////////////////////

    public void ShowDebugInfo ( Rect _pos ) {
        GUILayout.BeginArea( new Rect( _pos.x, _pos.y, _pos.width, _pos.height), "Debug", GUI.skin.window);

            // Mouse State
            GUILayout.Label( "Mouse State" );

            GUILayout.BeginHorizontal ();
            GUILayout.Space (15);
                GUILayout.BeginVertical ();
                    GUILayout.Label( "Current Pos: " + mouseState.currentPos.ToString() );

                    string buttons = (mouseState.currentButtons & exUIEvent.MouseButtonFlags.Left) != 0 ? "Left | " : "";
                    buttons += (mouseState.currentButtons & exUIEvent.MouseButtonFlags.Middle) != 0 ? "Middle | " : "";
                    buttons += (mouseState.currentButtons & exUIEvent.MouseButtonFlags.Right) != 0 ? "Right | " : "";
                    GUILayout.Label( "Current Buttons: " + buttons );

                    GUILayout.Label( "Hot Control: " + (mouseState.hotControl ? mouseState.hotControl.name : "None") );
                    GUILayout.Label( "Keyboard Control: " + (mouseState.keyboardControl ? mouseState.keyboardControl.name : "None") );

                GUILayout.EndVertical ();
            GUILayout.EndHorizontal ();

            // Root Controls
            GUILayout.Label( "Root Controls" );

            GUILayout.BeginHorizontal ();
            GUILayout.Space (15);
                GUILayout.BeginVertical ();
                    for ( int i = 0; i < rootControls.Count; ++i ) {
                        GUILayout.Label( "[" + i + "] " + rootControls[i].name );
                    }
                GUILayout.EndVertical ();
            GUILayout.EndHorizontal ();
        GUILayout.EndArea();
    }
}
