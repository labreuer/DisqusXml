using System;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DisqusXml
{
	class Program
	{
		const string Path = @"C:\Users\labreuer\Dropbox\Disqus\x.xml";
		//const string Path = @"C:\Users\labreuer\Dropbox\Disqus\simple.xml";

		static void Main(string[] args)
		{
			//FixXml();
			XmlDocument x = new XmlDocument();
			x.Load(Path.Replace("x.xml", "x2.xml"));
			//foreach (var n in x.SelectSingleNode(")
			var roots = x.DocumentElement.SelectNodes("li[contains(@class, 'post')]").Cast<XmlElement>();
			var rootPosts = roots.Select(n => new Post(n));
			var all = rootPosts.SelectMany(r => TraverseDepth(r)).ToArray();
			Console.WriteLine(Find(all, args[0], author: "Questioner"));
			//Console.WriteLine(ListResults(all, "free will"));

			//foreach (var p in all)
			//{
			//	if (Regex.IsMatch(p.Html, "free will"))
			//		Console.WriteLine(p.Author);
			//}

			return;



			var ns = SearchByAppendedClass(x, "//li", "post");
			Console.WriteLine(ns.Count());
			Console.WriteLine(ns
				//.Where(xe => SearchByAppendedClass(xe, "div[@data-role='post-content']//div", "post-message").Count() == 1)
				.Where(xe => xe.SelectNodes("div[@data-role='post-content']//div[@data-role='message']").Count == 1)
				.Count());



			var posts = ns.Select(n => new Post(n)).ToArray();


			var children = posts.Where(p => p.ParentUrl != null).GroupBy(p => p.ParentUrl);
			var d = posts.ToDictionary(p => p.Url);
			foreach (var g in children)
				d[g.Key].Children = g.ToArray();

			foreach (var n in ns)
			{
				Console.WriteLine(new Post(n).Author);
			}

			//Console.WriteLine(ns.Cast<XmlElement>().Select(xe => xe.LocalName).Join("\n"));

			//foreach (var n in ns.Cast<XmlElement>())
			//	if (Regex.IsMatch(n.InnerText, "free will", RegexOptions.IgnoreCase))
			//		Console.WriteLine(n);
			// [contains(text(), 'free will')]
		}

		private static string ListResults(Post[] all, string find)
		{
			return all
				.Where(p => !p.IsDeleted && p.Html.Contains(find, StringComparison.CurrentCultureIgnoreCase))
				.OrderBy(p => p.DatePosted)
				.Select(p => $"{p.DatePosted:yyyy-MM-dd HH:mm} {p.Id} {p.Author}{(p.Parent != null ? $" -> {p.Parent.Author}" : "")}")
				.Join("\n");
		}

		private static string Find(Post[] all, string find, string author = null, bool requireReplies = true)
		{
			return all
				.Where(p => !p.IsDeleted && p.Html.Contains(find, StringComparison.CurrentCultureIgnoreCase))
				.Where(p =>
					(author == null || p.Author.Equals(author, StringComparison.InvariantCultureIgnoreCase)) &&
					(!requireReplies || p.Children.Length > 0))
				.OrderBy(p => p.DatePosted)
				.SelectMany(p => SelectNodesCaseInvariant(p, p.HtmlNode, find)
					//.SelectNodes(".//*[contains(lower-case(text()), 'free will')]")
					//.Cast<XmlElement>()
					.Select(n => new { Post = p, Match = BlockLevelNonBlockquoteNode(n) }))
				//.Select(p => $"{p.Author}{(p.Parent != null ? $" -> {p.Parent.Author}" : "")} {p.DatePostedString}")
				.Where(e => e.Match != null)
				.SelectMany(e => Regex.Split(e.Match.InnerXml, @"<br\s*/>", RegexOptions.IgnoreCase)
					.Where(s => s.Contains(find, StringComparison.InvariantCultureIgnoreCase))
					.Select(s => $"<blockquote><a href=\"{e.Post.Url}\">{e.Post.Author}</a>@{e.Post.Parent?.Author}: {s}</blockquote>"))
				.Join("\n\n");
		}

		static HashSet<string> NonBlockTags = new HashSet<string>(new[] { "span", "a", "b", "i", "u", "s", "em", "strong", "strike", "#text" });

		static XmlNode BlockLevelNonBlockquoteNode(XmlNode xe)
		{
			while (NonBlockTags.Contains(xe.Name.ToLower()))
				xe = xe.ParentNode;

			var test = xe;
			while (test.Name.ToLower() != "div" || test.Attributes.Count == 0)
			{
				if (test.Name.ToLower() == "blockquote")
					return null;
				else
					test = test.ParentNode;
			}

			return xe;
		}

		static IEnumerable<XmlNode> SelectNodesCaseInvariant(Post p, XmlNode n, string find)
		{
			if (n is XmlText && n.InnerText.Contains(find, StringComparison.CurrentCultureIgnoreCase))
				yield return n;
			foreach (var c in n.ChildNodes)
				foreach (var cc in SelectNodesCaseInvariant(p, (XmlNode)c, find))
					yield return cc;
		}

		static IEnumerable<Post> TraverseDepth(Post p)
		{
			yield return p;
			foreach (var c in p.Children)
				foreach (var cc in TraverseDepth(c))
					yield return cc;
		}


		static IEnumerable<XmlElement> SearchByAppendedClass(XmlNode xn, string xpathPrefix, string cssClass)
		{
			return xn
				.SelectNodes($"{xpathPrefix}[contains(@class, '{cssClass}')]")
				.Cast<XmlElement>()
				.Where(xe => Regex.IsMatch(xe.GetAttribute("class"), $@"\b{cssClass}\b"));
		}

		static void FixXml()
		{
			XmlDocument x = new XmlDocument();
			var s = File.ReadAllText(Path);
			s = Regex.Replace(s, @"(?<=<(img|input|br)( [^>]+|))(?=>)", "/");
			s = @"
				<!DOCTYPE doctypeName [
				   <!ENTITY nbsp ' &#160;'>
				]>" + s;
			File.WriteAllText(@"C:\Users\labreuer\Dropbox\Disqus\x2.xml", s);
			x.LoadXml(s);
		}
	}

	public static class Functional
	{
		public static string Join(this IEnumerable<string> strings, string delimiter)
		{
			return strings.Any()
				? string.Join(delimiter, strings as string[] ?? strings)
				: "";
		}
	}
}