/*
 * Copyright 2019,2020 Sony Corporation
 */

using System.Runtime.CompilerServices;

#if UNITY_EDITOR

[assembly: InternalsVisibleTo("Unity.jp.co.sony.srd.Editor")]
[assembly: InternalsVisibleTo("Unity.jp.co.sony.srd.Editor.Tests")]

#endif

[assembly: InternalsVisibleTo("Unity.jp.co.sony.srd.Tests")]
[assembly: InternalsVisibleTo("Unity.jp.co.sony.srd.Scenes.Tests")]
