﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace Fuxion.Windows.Markup;

public class DisplayExtension : MarkupExtension
{
	public DisplayExtension(string bindExpression)
	{
#if DEBUG
		Printer = Fuxion.Printer.Default;
#endif
		var mode = DisplayMode.Name;
		if (bindExpression.StartsWith("["))
		{
			var modeStr = bindExpression.Split(']')[0].Substring(1);
			if (!Enum.TryParse(modeStr, true, out mode))
				throw new ArgumentException($"El valor de fuxion:Display '{bindExpression}' no es válido. No se puede parsear '{modeStr}' como una propiedad de '{nameof(DisplayAttribute)}'.");
			bindExpression = bindExpression.Split(']')[1];
		}
		var pros = bindExpression.Split('.');
		for (var i = pros.Length - 1; i >= 0; i--)
			chain.Add(new(pros[i], pro =>
			{
				var att                                          = pro?.GetCustomAttribute<DisplayAttribute>(true, false);
				var nonAttributeValue                            = pro?.Name;
				if (nonAttributeValue != null) nonAttributeValue = NonAttrributePrefix + nonAttributeValue + NonAttrributeSufix;
				string? attRes                                   = null;
				switch (mode)
				{
					case DisplayMode.Name:
						attRes = att?.GetName();
						break;
					case DisplayMode.Description:
						attRes = att?.GetDescription();
						break;
					case DisplayMode.GroupName:
						attRes = att?.GetGroupName();
						break;
					case DisplayMode.ShortName:
						attRes = att?.GetShortName();
						break;
					case DisplayMode.Order:
						attRes = att?.GetOrder()?.ToString();
						break;
					case DisplayMode.Prompt:
						attRes = att?.GetPrompt();
						break;
				}
				return attRes ?? nonAttributeValue;
			})
			{
				printer = Printer, NextLink = i == pros.Length - 1 ? null : chain.FirstOrDefault(l => l.PropertyName == pros[i + 1])
			});
		chain.Reverse();
		for (var i = 0; i < chain.Count; i++) chain[i].PreviousLink = i == 0 ? null : chain[i - 1];
	}
	IPrinter?                             _Printer;
	internal      List<NotifierChainLink> chain = new();
	public static string                  NonAttrributePrefix { get; set; } = "";
	public static string                  NonAttrributeSufix  { get; set; } = "";
	internal IPrinter? Printer
	{
		get => _Printer;
		set
		{
			_Printer = value;
			foreach (var link in chain) link.printer = value;
		}
	}
	public override object? ProvideValue(IServiceProvider serviceProvider)
	{
		if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget provider)
		{
			if (provider.TargetObject == null || provider.TargetProperty == null) return null;
			var first = chain.First();
			if (provider.TargetObject is FrameworkElement element)
			{
				first.TargetObject = element;
				element.DataContextChanged += (s, e) =>
				{
					Printer?.WriteLine("DataContextChanged: " + e.NewValue);
					first.DataContext = e.NewValue;
					first.SetValue();
				};
				first.DataContext = element.DataContext;
			} else if (provider.TargetObject is FrameworkContentElement contentElement)
			{
				first.TargetObject = contentElement;
				contentElement.DataContextChanged += (s, e) =>
				{
					Printer?.WriteLine("DataContextChanged: " + e.NewValue);
					first.DataContext = e.NewValue;
					first.SetValue();
				};
				first.DataContext = contentElement.DataContext;
			}
			first.TargetDependencyProperty = provider.TargetProperty as DependencyProperty;
			first.SetValue();
		}
		return null;
	}
}

enum DisplayMode
{
	Name,
	Description,
	GroupName,
	ShortName,
	Order,
	Prompt
}

class NotifierChainLink
{
	public NotifierChainLink(string propertyName, Func<PropertyInfo?, string?> getValueFunction)
	{
		this.getValueFunction = getValueFunction;
		PropertyName          = propertyName;
		EventHandler          = PropertyChanged;
	}
	readonly Func<PropertyInfo?, string?> getValueFunction;
	object? _DataContext;
	DependencyProperty? _TargetDependencyProperty;
	object? _TargetElement;
	internal IPrinter? printer;
	public   NotifierChainLink? NextLink { get; set; }
	public   NotifierChainLink? PreviousLink { get; set; }
	public   string PropertyName { get; set; }
	public   PropertyChangedEventHandler EventHandler { get; set; }
	public   INotifyPropertyChanged? ContextNotifier => DataContext != null ? typeof(INotifyPropertyChanged).IsAssignableFrom(DataContext.GetType()) ? (INotifyPropertyChanged?)Context : null : null;
	public object? DataContext
	{
		get => _DataContext;
		set
		{
			_DataContext = value;
			if (ContextNotifier != null) ContextNotifier.PropertyChanged += EventHandler;
		}
	}
	object?           Context          => DataContext ?? (PreviousLink?.Context != null ? PreviousLink?.ContextProperty?.GetValue(PreviousLink.Context) : null);
	Type?             ContextType      => Context?.GetType();
	PropertyInfo?     ContextProperty  => ContextType?.GetProperty(PropertyName);
	DisplayAttribute? ContextAttribute => ContextProperty?.GetCustomAttribute<DisplayAttribute>(true, false);
	public DependencyProperty? TargetDependencyProperty
	{
		get => _TargetDependencyProperty ?? PreviousLink?.TargetDependencyProperty;
		set => _TargetDependencyProperty = value;
	}
	public object? TargetObject
	{
		get => _TargetElement ?? PreviousLink?.TargetObject;
		set => _TargetElement = value;
	}
	public PropertyInfo? TargetProperty => TargetDependencyProperty?.Name != null ? TargetObject?.GetType().GetProperty(TargetDependencyProperty.Name) : null;
	void PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (DataContext != null)
			if (NextLink != null)
				NextLink.DataContext = ContextProperty?.GetValue(DataContext);
		SetValue();
	}
	public void SetValue()
	{
		printer?.WriteLine($"Setting value for {PropertyName}");
		if (NextLink == null)
		{
			printer?.WriteLine($"   DataContext: {DataContext?.ToString()           ?? "null"}");
			printer?.WriteLine($"   PreviousLink: {PreviousLink?.ToString()         ?? "null"}");
			printer?.WriteLine($"   Context: {Context?.ToString()                   ?? "null"}");
			printer?.WriteLine($"   ContextType: {ContextType?.Name                 ?? "null"}");
			printer?.WriteLine($"   ContextProperty: {ContextProperty?.Name         ?? "null"}");
			printer?.WriteLine($"   ContextAttribute: {ContextAttribute?.ToString() ?? "null"}");
			printer?.WriteLine($"   TargetElement: {TargetObject?.ToString()        ?? "null"}");
			printer?.WriteLine($"   TargetProperty: {TargetProperty?.Name           ?? "null"}");
			if (TargetObject != null) TargetProperty?.SetValue(TargetObject, getValueFunction(ContextProperty));
		} else
			NextLink.SetValue();
	}
	public override string? ToString() => PropertyName;
}