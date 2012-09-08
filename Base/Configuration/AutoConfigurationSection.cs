using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Reflection;

namespace CommonCore.Configuration
{
	internal class AutoConfigurationHelper
	{
		private static readonly ConcurrentDictionary<Type, Dictionary<ConfigurationProperty, PropertyInfo>> _TypeProperties =
			new ConcurrentDictionary<Type, Dictionary<ConfigurationProperty, PropertyInfo>>();
		private readonly Dictionary<ConfigurationProperty, PropertyInfo> _Properties;
		private readonly ConfigurationElement _ConfigElement;
		private readonly Action<ConfigurationProperty, object> _ValueSetter;
		private readonly Func<ConfigurationProperty, object> _ValueGetter;

		internal AutoConfigurationHelper(ConfigurationElement element, Action<ConfigurationProperty, object> valueSetter, Func<ConfigurationProperty, object> valueGetter)
		{
			_ConfigElement = element;
			_ValueSetter = valueSetter;
			_ValueGetter = valueGetter;

			var type = element.GetType();
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

			// Pre-initialize properties of type ConfigurationElement, or things go boom
			foreach (var property in _Properties)
			{
				if (typeof(ConfigurationElement).IsAssignableFrom(property.Value.PropertyType))
					property.Value.SetValue(_ConfigElement, _ValueGetter(property.Key), null);
			}
		}

		internal void PostDeserialize()
		{
			foreach (var property in _Properties)
			{
				property.Value.SetValue(_ConfigElement, _ValueGetter(property.Key), null);
			}
		}

		internal void PreSerialize(System.Xml.XmlWriter writer)
		{
			foreach (var property in _Properties)
			{
				_ValueSetter(property.Key, property.Value.GetValue(_ConfigElement, null));
			}
		}
	}

	/// <summary>
	/// A <see cref="System.Configuration.ConfigurationSection"/> which uses  <see cref="System.Configuration.ConfigurationPropertyAttribute"/>
	/// attributes on properties to automatically read from and write to an inheriting configuration section.
	/// </summary>
	public abstract class AutoConfigurationSection : ConfigurationSection
	{
		private readonly AutoConfigurationHelper _AutoConfigHelper;
		protected AutoConfigurationSection()
		{
			_AutoConfigHelper = new AutoConfigurationHelper(this, (c, v) => this[c] = v, c => this[c]);
		}

		protected override void PostDeserialize()
		{
			base.PostDeserialize();

			_AutoConfigHelper.PostDeserialize();
		}

		protected override void PreSerialize(System.Xml.XmlWriter writer)
		{
			base.PreSerialize(writer);

			_AutoConfigHelper.PreSerialize(writer);
		}
	}
}
