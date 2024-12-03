﻿using ActionEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("动画轨道")]
    [Description("这是一个播放动画剪辑的轨道")]
    [ShowIcon(typeof(AnimationClip))]
    [Color(0.48f, 0.71f, 0.84f)]
    [Attachable(typeof(SkillAsset))]
    public class AnimationTrack : Track
    {
        
    }
}