#if TOOLS // For release builds, we only need the node definition, not the whole plugin

using System;
using System.Collections.Generic;
using System.Linq;

using Godot;
using Godot.Collections;

using Lavabird.Plugins.AnimatedAseprite;
using Lavabird.Plugins.AnimatedAseprite.Importer;

using Newtonsoft.Json;

// Plugin needs to be outside namespace

/// <summary>
/// Import plugin that processes exported Aseprite animation data and saves it as a Resouce
/// to be used by Godot to render animations.
/// </summary>
[Tool]
internal partial class AseJsonImportPlugin : EditorImportPlugin
{
	/// <inheritdoc/>
	public override string _GetImporterName()
	{
		return "Lavabird.AnimatedAseprite.AseJsonImporter";
	}

	/// <inheritdoc/>
	public override string _GetVisibleName()
	{
		return "Aseprite Animation Json";
	}

	/// <inheritdoc/>
	public override string[] _GetRecognizedExtensions()
	{
		// We rely on a custom extension so that we don't process every JSON file in the project.
		// It's a common format and this avoids collisions.
		return [ "ase-json" ];
	}

	/// <inheritdoc/>
	public override string _GetSaveExtension()
	{
		return "res";
	}

	/// <inheritdoc/>
	public override string _GetResourceType()
	{
		return "Resource";
	}

	/// <inheritdoc/>
	public override int _GetPresetCount()
	{
		// We have no presets
		return 0;
	}

	/// <inheritdoc/>
	public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
	{
		// Must return even if we don't have options
		return [];
	}

	/// <inheritdoc/>
	public override Error _Import(string sourceFile, string savePath, Dictionary options, 
		Array<string> array, Array<string> genFiles1)
	{
		// Grab the contents of the JSON file
		using var file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			var openError = FileAccess.GetOpenError();
			AnimatedAsepritePlugin.Error($"Unable to open animation definition file '{sourceFile}'. Got error {openError}"); 
			return Error.FileCantRead;
		}

		var json = file.GetAsText();
		file.Close();

		// Parse the JSON into our strongly typed container
		AseJsonData jsonData;
		try
		{
			var settings = new JsonSerializerSettings
			{ 
				ContractResolver = new RequireAllPropertiesResolver() 
			};
			jsonData = JsonConvert.DeserializeObject<AseJsonData>(json, settings)!;
		}
		catch(JsonException ex)
		{
			AnimatedAsepritePlugin.Error($"Unable parse animation JSON file. {ex.Message}");
			return Error.FileCorrupt;
		}

		var animations = ParseAseJsonData(jsonData);

		// Did we have anything to export?
		if (!animations.Animations.Any())
		{
			AnimatedAsepritePlugin.Error($"No valid animations found in file '{sourceFile}'.");
			return Error.InvalidData;
		}

		// Everything is OK, try and save
		savePath = $"{savePath}.{_GetSaveExtension()}";
		var err = ResourceSaver.Save(animations, savePath);
		if (err != Error.Ok)
		{
			AnimatedAsepritePlugin.Error($"Unable to save animation resource file '{savePath}'. Got error {err}");
			return err;
		}

		return Error.Ok;
	}

	/// <summary>
	/// Parses and validates an AseJsonData object to extract the information we needed for a AsepriteAnimations resource.
	/// </summary>
	private AsepriteAnimations ParseAseJsonData(AseJsonData aseJsonData)
	{
		var animations = InstancePluginResource<AsepriteAnimations>();

		// We can have no frame tags. This is a special case where the whole file is a single animation
		aseJsonData.Meta.FrameTags ??= new List<AseJsonData.FrameTagInfo>();
		if (aseJsonData.Meta.FrameTags.Count == 0)
		{
			// Add a fake frameTag for a "default" animtion with all frames included, so we can re-use our parser code
			aseJsonData.Meta.FrameTags.Add(new AseJsonData.FrameTagInfo()
			{
				Name = "default",
				From = 0,
				To = aseJsonData.Frames.Count - 1,
				Direction = "forward"
			});
		}

		// Parse each tagged animation into our AsepriteAnimations resource
		if (aseJsonData.Frames is { Count: > 0 })
		{
			foreach (var tag in aseJsonData.Meta.FrameTags)
			{
				// Sanity check referenced frames are in range
				if (tag.From < 0 || tag.From > aseJsonData.Frames.Count - 1 ||
					tag.To < 0 || tag.To > aseJsonData.Frames.Count - 1 || tag.From > tag.To)
				{
					AnimatedAsepritePlugin.Error(
						$"Invalid frame range ({tag.From} -> {tag.To}) given for animation '{tag.Name}'. Ignoring.");
					continue;
				}

				// Each tag defines a named animation with a play direction
				var animation = InstancePluginResource<AsepriteAnimation>();
				AsepriteAnimation.AnimationDirection direction;
				if (!Enum.TryParse(tag.Direction, true, out direction))
				{
					direction = AsepriteAnimation.AnimationDirection.Forward;
					GD.PushWarning($"Unrecognised animation direction '{tag.Direction}'. Defaulting to forward.");
				}
				animation.Direction = direction;

				// Each animation will have a set of frames
				for (var i = tag.From; i <= tag.To; i++)
				{
					var frame = InstancePluginResource<AsepriteFrame>();

					frame.Region = new Rect2(aseJsonData.Frames[i].Frame.X, aseJsonData.Frames[i].Frame.Y,
						aseJsonData.Frames[i].Frame.W, aseJsonData.Frames[i].Frame.H);
					frame.Offset = new Vector2(
						aseJsonData.Frames[i].SpriteSourceSize.X, aseJsonData.Frames[i].SpriteSourceSize.Y);
					frame.Duration = aseJsonData.Frames[i].Duration / 1000f; // Aseprite uses ms, we use seconds

					animation.Frames.Add(frame);
				}

				// We assume the same frame (source) size for every frame in the animation
				animation.FrameSize = new Vector2(
					aseJsonData.Frames[tag.From].SourceSize.W, aseJsonData.Frames[tag.From].SourceSize.H);

				// We need to strip any commas from the name. They will break Godot's property listing in the inspector
				var animationName = tag.Name.Replace(",", "");
				animations.AddAnimation(animationName, animation);
			}
		}

		return animations;
	}

	/// <summary>
	/// Creates a new custom Resource with the script property set correctly.
	/// </summary>
	private T InstancePluginResource<T>() where T : Resource
	{
		// C# custom resources are janky. We need to instance them via the CSharpScript, which means we need the script
		// path. In G4 this works correctly with `new` so this method can be removed.
		return (T)GD.Load<CSharpScript>($"{AnimatedAsepritePlugin.PluginRoot}/{typeof(T).Name}.cs").New();
	}
}

#endif