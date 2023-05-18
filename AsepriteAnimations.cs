using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

namespace Lavabird.Plugins.AnimatedAseprite;

/// <summary>
/// Collection of Animation objects that define all the aniamtions from an Aseprite export.
/// </summary>
[Tool]
public class AsepriteAnimations : Resource
{
	/// <summary>
	/// Gets the list of all animations in this AnimationData object.
	/// </summary>
	public IEnumerable<AsepriteAnimation> Animations { get => AnimationMap.Values; }

	/// <summary>
	/// Gets the the names of all the animations in this AnimationData object.
	/// </summary>
	public IEnumerable<string> AnimationNames { get => AnimationMap.Keys; }

	/// <summary>
	/// Map of all animations we have stored keyed by animation name.
	/// </summary>
	[Export]
	private Godot.Collections.Dictionary<string, AsepriteAnimation> AnimationMap;

	public AsepriteAnimations()
	{
		AnimationMap = new Godot.Collections.Dictionary<string, AsepriteAnimation>();
	}

	/// <summary>
	/// Adds a new animation to the FrameData object.
	/// </summary>
	public void AddAnimation(string animationName, AsepriteAnimation animation)
	{
		AnimationMap.Add(animationName, animation);
	}

	/// <summary>
	/// Checks if an animation with the given name exists in this collection.
	/// </summary>
	public bool HasAnimation(string animationName)
	{
		return AnimationMap.ContainsKey(animationName);
	}

	/// <summary>
	/// Returns the Animation with the given name.
	/// </summary>
	public AsepriteAnimation this[string animationName]
	{
		get => AnimationMap[animationName];
	}
}
