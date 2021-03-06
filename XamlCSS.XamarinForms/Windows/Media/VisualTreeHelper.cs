﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace XamlCSS.Windows.Media
{
	public static class VisualTreeHelper
	{
		public static event EventHandler SubTreeAdded;
		public static event EventHandler SubTreeRemoved;

		private readonly static ConditionalWeakTable<Element, List<Element>> parentChildAssociations =
			new ConditionalWeakTable<Element, List<Element>>();

		private readonly static ConditionalWeakTable<Element, Element> childParentAssociations =
			new ConditionalWeakTable<Element, Element>();

		public static string PrintRealPath(Element e)
		{
			if (e == null)
			{
				return "";
			}

			return $".({e.GetType().Name} {e.Id}).{GetRealParent(e)}";
		}

		public static string GetRealParent(Element e)
		{
			var realParent = e.GetType().GetRuntimeProperties().Single(x => x.Name == "RealParent").GetValue(e) as Element;

			if (realParent == null)
			{
				return $"(ROOT)";
			}

			return GetRealParent(realParent) + $".({realParent.GetType().Name} {realParent.Id})";
		}

		public static void Initialize(Element root)
		{
			AttachedChild(root);
		}

		public static void Exclude(Element cell)
		{
			RemoveChildInternal(cell);
		}

		public static void Include(Element cell)
		{
			if (AttachedChild(cell))
			{
				SubTreeAdded?.Invoke(cell, new EventArgs());
			}
		}

		public static IEnumerable<Element> GetChildren(Element e)
		{
			List<Element> list = null;
			if (parentChildAssociations.TryGetValue(e, out list))
			{
				return list;
			}

			return Enumerable.Empty<Element>();
		}

		public static Element GetParent(Element e)
		{
			return e.Parent;
		}

		private static bool AttachedChild(Element child)
		{
			if (child == null)
			{
				return false;
			}

			Element dummy = null;
			if (childParentAssociations.TryGetValue(child, out dummy))
			{
				return false;
			}

			if (child.Parent != null)
			{
				var p = child.Parent;

				List<Element> list = null;
				if (parentChildAssociations.TryGetValue(p, out list) == false)
				{
					list = new List<Element>();
					parentChildAssociations.Add(p, list);
				}

				list?.Add(child);

				try
				{
					childParentAssociations.Add(child, p);
				}
				catch { }
			}

			if (child is ViewCell)
			{
				var cell = child as ViewCell;

				AttachedChild(cell.View);
			}

			var layout = child as ILayoutController;
			if (layout != null)
			{
				foreach (var i in layout.Children)
				{
					AttachedChild(i);
				}
			}

			var contentPage = child as ContentPage;
			if (contentPage != null)
			{
				AttachedChild(contentPage.Content);
			}

			var navigationPage = child as NavigationPage;
			if (navigationPage != null)
			{
				AttachedChild(navigationPage.CurrentPage);
			}

			var masterDetailPage = child as MasterDetailPage;
			if (masterDetailPage != null)
			{
				AttachedChild(masterDetailPage.Master);
				AttachedChild(masterDetailPage.Detail);
			}

			var app = child as Application;
			if (app != null)
			{
				AttachedChild(app.MainPage);

				app.ModalPushing += App_ModalPushing;
				app.ModalPopped += App_ModalPopped;
			}

			child.ChildAdded += ChildAddedHandler;
			child.ChildRemoved += ChildRemovedHandler;

			return true;
		}

		private static void App_ModalPushing(object sender, ModalPushingEventArgs e)
		{
			e.Modal.Parent = sender as Application;

			AttachChildInternal(e.Modal);
		}

		private static void App_ModalPopped(object sender, ModalPoppedEventArgs e)
		{
			RemoveChildInternal(e.Modal);
		}

		private static void ChildRemovedHandler(object sender, ElementEventArgs e)
		{
			RemoveChildInternal(e.Element);
		}
		private static void RemoveChildInternal(Element element)
		{
			if (CanUnattachChild(element))
			{
				UnattachedChild(element);

				SubTreeRemoved?.Invoke(element, new EventArgs());
			}
		}

		private static void AttachChildInternal(Element element)
		{
			if (AttachedChild(element))
			{
				SubTreeAdded?.Invoke(element, new EventArgs());
			}
		}

		private static void ChildAddedHandler(object sender, ElementEventArgs e)
		{
			AttachChildInternal(e.Element);
		}

		private static bool CanUnattachChild(Element child)
		{
			Element dummy = null;

			if (child == null)
			{
				return false;
			}

			if (!childParentAssociations.TryGetValue(child, out dummy))
			{
				return false;
			}

			return true;
		}
		private static bool UnattachedChild(Element child)
		{
			Element dummy = null;

			if (child == null)
			{
				return false;
			}

			if (!childParentAssociations.TryGetValue(child, out dummy))
			{
				return false;
			}

			child.ChildAdded -= ChildAddedHandler;
			child.ChildRemoved -= ChildRemovedHandler;

			if (child is ViewCell)
			{
				var cell = child as ViewCell;

				UnattachedChild(cell.View);
			}

			var layout = child as ILayoutController;
			if (layout != null)
			{
				foreach (var i in layout.Children)
				{
					UnattachedChild(i);
				}
			}

			var contentPage = child as ContentPage;
			if (contentPage != null)
			{
				UnattachedChild(contentPage.Content);
			}

			var navigationPage = child as NavigationPage;
			if (navigationPage != null)
			{
				UnattachedChild(navigationPage.CurrentPage);
			}

			var masterDetailPage = child as MasterDetailPage;
			if (masterDetailPage != null)
			{
				UnattachedChild(masterDetailPage.Master);
				UnattachedChild(masterDetailPage.Detail);
			}

			var app = child as Application;
			if (app != null)
			{
				UnattachedChild(app.MainPage);

				app.ModalPushing -= App_ModalPushing;
				app.ModalPopped -= App_ModalPopped;
			}

			Element parent = null;

			childParentAssociations.TryGetValue(child, out parent);
			if (parent == null)
			{
				parent = child.Parent;
			}

			childParentAssociations.Remove(child);

			if (parent != null)
			{
				List<Element> list;
				if (!parentChildAssociations.TryGetValue(parent, out list))
				{
					return true;
				}

				list.Remove(child);

				if (list.Count == 0)
				{
					parentChildAssociations.Remove(parent);
				}
			}

			return true;
		}
	}
}
