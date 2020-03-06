using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace DisqusXml
{
	[System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	sealed class XpathAttribute : Attribute
	{
		public readonly string Xpath;
		public readonly string Attribute;

		public XpathAttribute(string xpath, string attribute = null)
		{
			this.Xpath = xpath;
			this.Attribute = attribute;
		}
	}

	class Post
	{
#pragma warning disable 0649
		public readonly XmlElement Node;
		[Xpath(".//header//a[@data-action='profile']", "data-username")]
		public readonly string Username;
		[Xpath(".//header//a[@data-action='profile']")]
		public readonly string Author;
		[Xpath(".//a[@data-role='relative-time']", "href")]
		public readonly string Url;
		[Xpath(".//a[@data-role='parent-link']", "href")]
		public readonly string ParentUrl;
		[Xpath(".//ul[@data-post-id]", "data-post-id")]
		public readonly string Id;
		[Xpath(".//a[@data-role='relative-time']", "title")]
		public readonly string DatePostedString;
		[Xpath(".//div[@data-role='message']/div")]
		public readonly string Html;
#pragma warning restore 0649

		public readonly DateTime DatePosted;
		public bool IsDeleted => DatePostedString == null;
		public readonly XmlElement HtmlNode;

		public Post[] Children;
		public Post Parent;

		public Post(XmlElement node)
		{
			Node = node;
			var x = node.SelectSingleNode("div[@data-role='post-content']");
			foreach (var fi in typeof(Post).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
			{
				foreach (var a in fi.GetCustomAttributes(typeof(XpathAttribute), false))
				{
					if (a is XpathAttribute xa)
					{
						var xc = x.SelectSingleNode(xa.Xpath);
						if (xc != null)
							fi.SetValue(this, xa.Attribute == null ? xc.InnerXml : xc.Attributes[xa.Attribute].Value);
					}
				}
			}

			if (DatePostedString != null)
				DatePosted = DateTime.Parse(DatePostedString);

			HtmlNode = (XmlElement)x.SelectSingleNode(".//div[@data-role='message']/div");

			Children = node
				.SelectNodes("div[contains(@class, 'children')]/ul[@data-role='children']/li[contains(@class, 'post')]")
				.Cast<XmlElement>()
				.Select(n => new Post(n))
				.ToArray();
			foreach (var c in Children)
				c.Parent = this;
		}

	}
}
