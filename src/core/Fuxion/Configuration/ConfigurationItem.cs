using Fuxion.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace Fuxion.Configuration
{
	public abstract class ConfigurationItem<TConfigurationItem> : Notifier<TConfigurationItem> where TConfigurationItem : class, INotifier<TConfigurationItem>, new()
	{
		[JsonIgnore]
		public abstract Guid ConfigurationItemId { get; }
	}
}
