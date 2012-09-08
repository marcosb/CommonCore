using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Reflection;

namespace CommonCore.Configuration
{
	/// <summary>
	/// A <see cref="System.Configuration.ConfigurationSection"/> which uses  <see cref="System.Configuration.ConfigurationPropertyAttribute"/>
	/// attributes on properties to automatically read from and write to an inheriting configuration section.
	/// </summary>
	public abstract class AutoConfigurationSection : ConfigurationSection
	{
		private static readonly ConcurrentDictionary<Type, Dictionary<ConfigurationProperty, PropertyInfo>> _TypeProperties =
			new ConcurrentDictionary<Type, Dictionary<ConfigurationProperty, PropertyInfo>>();
		private readonly Dictionary<ConfigurationProperty, PropertyInfo> _Properties;

		protected AutoConfigurationSection()
		{
			var type = GetType();
			Dictionary<ConfigurationProperty, PropertyInfo> properties;
			if (!_TypeProperties.TryGetValue(type, out properties))
			{
				properties = new Dictionary<ConfigurationProperty, PropertyInfo>();
				foreach (var member in type.GetProperties())
				{
					var configField = member.GetCustomAttributes(typeof(ConfigurationPropertyAttribute), true).Cast<ConfigurationPropertyAttribute>().FirstOrDefault();
					if (configField != null)
					{
						var property = new ConfigurationProperty(configField.Name, member.PropertyType, configField.DefaultValue, ConfigurationPropertyOptions.None);
						properties[property] = member;
					}
				}
				_TypeProperties.TryAdd(type, properties);
			}
			_Properties = properties;

			// Pre-initialize properties of type ConfigurationElementCollection, or things go boom
			foreach (var property in _Properties)
			{
				if (typeof(ConfigurationElementCollection).IsAssignableFrom(property.Value.PropertyType))
					property.Value.SetValue(this, this[property.Key], null);
			}
		}

		protected override void PostDeserialize()
		{
			base.PostDeserialize();

			foreach (var property in _Properties)
			{
				property.Value.SetValue(this, this[property.Key], null);
			}
		}

		protected override void PreSerialize(System.Xml.XmlWriter writer)
		{
			base.PreSerialize(writer);

			foreach (var property in _Properties)
			{
				this[property.Key] = property.Value.GetValue(this, null);
			}
		}
	}
}
