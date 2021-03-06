﻿using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml.Linq;

namespace XamlCSS.WPF
{
	public class MarkupExtensionParser : IMarkupExtensionParser
	{
		public object Parse(string expression)
		{
			string myBindingExpression = expression;
			var test = "<TextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" DataContext=\"" + myBindingExpression + "\" />";

			var result = XamlReader.Parse(test) as TextBlock;

			var bindingExpression = result.ReadLocalValue(TextBlock.DataContextProperty);
			var binding = bindingExpression;

			if (binding is BindingExpression)
			{
				binding = ((BindingExpression)binding).ParentBinding;
			}

			return binding;
		}
		public object Parse(string expression, ResourceDictionary resourceDictionary)
		{
			string myBindingExpression = expression;

			var resDictString = resourceDictionary != null ? XamlWriter.Save(resourceDictionary) : "";
			string inner = null;
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(resDictString)))
			{
				var doc = XDocument.Parse(resDictString);
				
				inner = doc.Descendants().First().Descendants().FirstOrDefault()?.ToString();
			}
			
			var test = $@"
<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
	<StackPanel.Resources>
		{ inner ?? "" }
	</StackPanel.Resources>
	<TextBlock DataContext=""{myBindingExpression}"" />
</StackPanel>";

			var result = (XamlReader.Parse(test) as StackPanel).Children[0];
			var bindingExpression = result.ReadLocalValue(TextBlock.DataContextProperty);

			var binding = bindingExpression;

			if (binding is BindingExpression)
			{
				binding = ((BindingExpression)binding).ParentBinding;
			}

			return binding;
		}

		public object ProvideValue(string expression, object obj)
		{
			ResourceDictionary resDict = null;

			if (obj is FrameworkElement)
			{
				resDict = (obj as FrameworkElement).Resources;
			}
			else if (obj is FrameworkContentElement)
			{
				resDict = (obj as FrameworkContentElement).Resources;
			}

            object binding = null;
            if (expression.Contains("Binding "))
            {
                binding = Parse(expression);
            }
            else
            {
                binding = Parse(expression);
            }

			if (binding is Binding)
			{
				return (binding as Binding).ProvideValue(null);
			}

			if (binding.GetType().Name == "ResourceReferenceExpression")
			{
				var a = binding.GetType().GetProperty("ResourceKey");

				return new DynamicResourceExtension(a.GetValue(binding));
			}

			return binding;
		}
	}
}
