using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Live2D.Cubism.Framework.Motion;
using UnityEngine;

public class MotionPlayer : MonoBehaviour
{
    CubismMotionController _motionController;
    [SerializeField] private AnimationClip animation;

    private void Start()
    {
        Application.targetFrameRate = 60;
        _motionController = GetComponent<CubismMotionController>();
    }

    public void PlayMotion()
    {
        if ((_motionController == null) || (animation == null))
        {
            return;
        }
        _motionController.PlayAnimation(animation, isLoop: false, priority: CubismMotionPriority.PriorityForce);
    }
    
}

