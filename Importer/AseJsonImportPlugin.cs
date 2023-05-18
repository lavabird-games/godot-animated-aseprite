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
internal class AseJsonImportPlugin : EditorImportPlugin
{
	/// <inheritdoc/>
	public override string GetImporterName()
	{
		return "Lavabird.AnimatedAseprite.AseJsonImporter";
	}

	/// <inheritdoc/>
	public override string GetVisibleName()
	{
		return "Aseprite Animation Json";
	}

	/// <inheritdoc/>
	public override Godot.Collections.Array GetRecognizedExtensions()
	{
		// We rely on a custom extension so that we don't process every JSON file in the project.
		// It's a common format and this avoids collisions.
		return new Godot.Collections.Array() { "ase-json" };
	}

	/// <inheritdoc/>
	public override string GetSaveExtension()
	{
		return "res";
	}

	/// <inheritdoc/>
	public override string GetResourceType()
	{
		return "Resource";
	}

	/// <inheritdoc/>
	public override int GetPresetCount()
	{
		// We have no presets
		return 0;
	}

	/// <inheritdoc/>
	public override Godot.Collections.Array GetImportOptions(int preset)
	{
		// Must return even if we don't have options
		return new Godot.Collections.Array();
	}

	/// <inheritdoc/>
	public override int Import(string sourceFile, string savePath, Dictionary options, 
		Godot.Collections.Array platformVariants, Godot.Collections.Array genFiles)
	{
		// Grab the contents of the JSON file
		var file = new File();
		var err = file.Open(sourceFile, File.ModeFlags.Read);
		if (err != Error.Ok)
		{
			AnimatedAsepritePlugin.Error($"Unable to open animation definition file '{sourceFile}'. Got error {err}");
			return (int)Error.FileCantRead;
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
			return (int)Error.FileCorrupt;
		}

		var animations = ParseAseJsonData(jsonData);

		// Did we have anything to export?
		if (animations.Animations.Count() == 0)
		{
			AnimatedAsepritePlugin.Error($"No valid animations found in file '{sourceFile}'.");
			return (int)Error.InvalidData;
		}

		// Everything is OK, try and save
		savePath = $"{savePath}.{GetSaveExtension()}";
		err = ResourceSaver.Save(savePath, animations);
		if (err != Error.Ok)
		{
			AnimatedAsepritePlugin.Error($"Unable to save animation resource file '{savePath}'. Got error {err}");
			return (int)err;
		}

		return (int)Error.Ok;
	}

	/// <summary>
	/// Parses and validates an AseJsonData object to extract the information we needed for a AsepriteAnimations resource.
	/// </summary>
	private AsepriteAnimations ParseAseJsonData(AseJsonData aseJsonData)
	{
		var animations = InstancePluginResource<AsepriteAnimations>();

		// We can have no frame tags. This is a special case where the whole file is a single animation
		if (aseJsonData.Meta.FrameTags == null)
		{
			aseJsonData.Meta.FrameTags = new List<AseJsonData.FrameTagInfo>();
		}
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
		if (aseJsonData.Frames != null && aseJsonData.Frames.Count > 0)
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

					frame.Region = new Rect2(aseJsonData.Frames[i].Frame.x, aseJsonData.Frames[i].Frame.y,
						aseJsonData.Frames[i].Frame.w, aseJsonData.Frames[i].Frame.h);
					frame.Offset = new Vector2(
						aseJsonData.Frames[i].SpriteSourceSize.x, aseJsonData.Frames[i].SpriteSourceSize.y);
					frame.Duration = aseJsonData.Frames[i].Duration / 1000f; // Aseprite uses ms, we use seconds

					animation.Frames.Add(frame);
				}

				// We assume the same frame (source) size for every frame in the animation
				animation.FrameSize = new Vector2(
					aseJsonData.Frames[tag.From].SourceSize.w, aseJsonData.Frames[tag.From].SourceSize.h);

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