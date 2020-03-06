using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DisqusXml
{
	class TagClosingTextReader : TextReader
	{
		private StreamReader _sr;
		private const int InitBuffLength = 1000;
		private char[] _buff = new char[InitBuffLength];
		int _start = 0; // inclusive
		int _end = 0; // exclusive

		private int BuffLength => (_end - _start + _buff.Length) % _buff.Length;

		public TagClosingTextReader(string path)
		{
			_sr = new StreamReader(path);
		}

		public override int Read(char[] buffer, int index, int count)
		{
			if (_start > _end)
			{
				int copy = Math.Min(_buff.Length - _start, count);
				Array.Copy(_buff, _start, buffer, index, copy);
				_start = (_start + copy) % _buff.Length;
				if (copy == count)
					return copy;
				count -= copy;
				index += copy;
			}
			if (_start < _end)
			{
				int copy = Math.Min(_end - _start, count);
				Array.Copy(_buff, _start, buffer, index, copy);
				_start = (_start + copy);
				if (copy == count)
					return copy;
				count -= copy;
				index += copy;
			}

			_sr.Read(buffer, index, count);




			//int buffRead = Math.Min(count, BuffLength > 0 ? _buff.Peek().Length - _buffIndex : 0);



			return base.Read(buffer, index, count);
		}

		public override void Close()
		{
			_sr.Close();
			base.Close();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_sr.Dispose();
			base.Dispose(disposing);
		}
	}
}
