using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lavabird.Plugins.AnimatedAseprite.Importer;

/// <summary>
/// Resolver that forces all properties to be required unless opt-ed out. Makes the data class less decorated.
/// </summary>
public class RequireAllPropertiesResolver : DefaultContractResolver
{
	protected override JsonObjectContract CreateObjectContract(Type objectType)
	{
		var contract = base.CreateObjectContract(objectType);
		contract.ItemRequired = Required.Always;
		return contract;
	}
}
