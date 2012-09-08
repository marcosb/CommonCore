using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace CommonCore.Configuration
{
	/// <summary>
	/// A <see cref="System.Configuration.AutoConfigurationElement"/> which uses  <see cref="System.Configuration.ConfigurationPropertyAttribute"/>
	/// attributes on properties to automatically read from and write to an inheriting configuration section.
	/// </summary>
	public abstract class AutoConfigurationElement : ConfigurationElement
	{
		private readonly AutoConfigurationHelper _AutoConfigHelper;
		protected AutoConfigurationElement()
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
