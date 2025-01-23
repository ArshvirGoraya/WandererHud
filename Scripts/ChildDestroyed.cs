using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildDestroyed : MonoBehaviour
{
    void OnDestroy(){
        Destroy(transform.parent.parent.gameObject);
    }
}
