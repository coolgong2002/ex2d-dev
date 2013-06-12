using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//@TODO: �ֶ����SpriteMngʱ�����Camera���ԣ������������������

//[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class ex2DMng : MonoBehaviour {

    [SerializeField]
    private static ex2DMng _instance;
    public static ex2DMng instance {
        get {
            if (!_instance) {
                GameObject go = new GameObject("2D Manager");
                Camera camera = go.AddComponent<Camera>();
                _instance = go.AddComponent<ex2DMng>();
                go.hideFlags = HideFlags.DontSave | exReleaseFlag.notEditable;
                camera.orthographic = true;
                camera.orthographicSize = Screen.height;
                go.SetActive(true);
            }
            return _instance;
        }
    }
    
    List<exLayer> allLayers = new List<exLayer>();

    public void Add(exLayer layer) {
        if (!allLayers.Contains(layer)) {
            allLayers.Add(layer);
        }
    }

    public void Remove(exLayer layer) {
        allLayers.Remove(layer);
    }

    void Awake () {
        _instance = this;
    }
	
    void LateUpdate() { 
        //@TODO: �����⵽��Ļ��С�ı䣬ͬ������Camera��orthographicSize
        foreach (exLayer layer in allLayers) {
            layer.UpdateMesh();
        }
    }
}
