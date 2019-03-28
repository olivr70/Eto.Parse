using System;
using System.Text;
using System.Globalization;

namespace Eto.Parse.Parsers
{
	public class StringParser : Parser
	{
		string quoteCharString;
		char[] quoteCharacters;
		string endQuoteCharString;
		char[] endQuoteCharacters;

		public char[] QuoteCharacters
		{
			get { return BeginQuoteCharacters; }
			set
			{
				BeginQuoteCharacters = EndQuoteCharacters = value;
			}
		}

		public char[] BeginQuoteCharacters
		{
			get { return quoteCharacters; }
			set
			{
				quoteCharacters = value;
				quoteCharString = value != null ? new string(value) : null;
			}
		}

		public char[] EndQuoteCharacters
		{
			get { return endQuoteCharacters; }
			set
			{
				endQuoteCharacters = value;
				endQuoteCharString = value != null ? new string(value) : null;
			}
		}

		public bool AllowEscapeCharacters { get; set; }

		public bool AllowDoubleQuote { get; set; }

		public bool AllowNonQuoted { get; set; }

		public Parser NonQuotedLetter { get; set; }

		public bool AllowQuoted
		{
			get { return quoteCharString != null; }
		}

		public override string DescriptiveName
		{
			get { return AllowQuoted ? "Quoted String" : "String"; }
		}

		public override object GetValue(string text)
		{
			if (text.Length > 0)
			{
				// process escapes using string format with no parameters
				if (AllowEscapeCharacters)
				{
					return GetEscapedString(text);
				}
				else if (AllowQuoted)
				{
					var quoteIndex = quoteCharString.IndexOf(text[0]);
					if (quoteIndex >= 0)
					{
						var quoteChar = endQuoteCharString[quoteIndex];
						if (text.Length >= 2 && text[text.Length - 1] == quoteChar)
						{
							text = text.Substring(1, text.Length - 2);
						}
						if (AllowDoubleQuote)
						{
							text = text.Replace(quoteChar.ToString() + quoteChar, quoteChar.ToString());
						}
					}
				}
			}
			return text;
		}

		string GetEscapedString(string source)
		{
			int pos = 0;
			var length = source.Length;
			var parseDoubleQuote = false;
			char quoteChar = default(char);
			if (AllowQuoted && length > 1)
			{
				var quoteIndex = quoteCharString.IndexOf(source[pos]);
				if (quoteIndex >= 0)
				{
					quoteChar = endQuoteCharString[quoteIndex];
					if (source[length - 1] == quoteChar)
					{
						pos++;
						length--;
						parseDoubleQuote = AllowDoubleQuote;
					}
				}
			}
			var str = new char[length];
			var newpos = 0;
			//var sb = new StringBuilder(length);
			while (pos < length)
			{
				char c = source[pos];
				if (c != '\\')
				{
					pos++;
					str[newpos++] = c;
					//sb.Append(c);
					// assume that the parse match ensured that we have a duplicate if we're not at the end of the string
					if (!parseDoubleQuote || c != quoteChar || pos >= length)
						continue;
					pos++;
					continue;
				}
				pos++;
				if (pos >= length)
					throw new ArgumentException("Missing escape sequence");
				switch (source[pos])
				{
					case 'n':
						c = '\n';
						break;
					case 'r':
						c = '\r';
						break;
					case '\'':
						c = '\'';
						break;
					case '\"':
						c = '\"';
						break;
					case '\\':
						c = '\\';
						break;
					case '0':
						c = '\0';
						break;
					case 'a':
						c = '\a';
						break;
					case 'b':
						c = '\b';
						break;
					case 'f':
						c = '\f';
						break;
					case 't':
						c = '\t';
						break;
					case 'v':
						c = '\v';
						break;
					case 'x':
						var hex = new StringBuilder(4);
						pos++;
						if (pos >= length)
							throw new ArgumentException("Missing escape sequence");
						for (int i = 0; i < 4; i++)
						{
							c = source[pos];
							if (!(char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
								break;
							hex.Append(c);
							pos++;
							if (pos > length)
								break;
						}
						if (hex.Length == 0)
							throw new ArgumentException("Unrecognized escape sequence");
						c = (char)Int32.Parse(hex.ToString(), NumberStyles.HexNumber);
						pos--;
						break;
					case 'u':
						pos++;
						if (pos + 3 >= length)
							throw new ArgumentException("Unrecognized escape sequence");
						try
						{
							uint charValue = UInt32.Parse(source.Substring(pos, 4), NumberStyles.HexNumber);
							c = (char)charValue;
							pos += 3;
						}
						catch (Exception)
						{
							throw new ArgumentException("Unrecognized escape sequence");
						}
						break;
					case 'U':
						pos++;
						if (pos + 7 >= length)
							throw new ArgumentException("Unrecognized escape sequence");
						try
						{
							uint charValue = UInt32.Parse(source.Substring(pos, 8), NumberStyles.HexNumber);
							if (charValue > 0xffff)
								throw new ArgumentException("Unrecognized escape sequence");
							c = (char)charValue;
							pos += 7;
						}
						catch (Exception)
						{
							throw new ArgumentException("Unrecognized escape sequence");
						}
						break;
					default:
						throw new ArgumentException("Unrecognized escape sequence");
				}
				pos++;
				str[newpos++] = c;
				//sb.Append(c);
			}

			return new string(str, 0, newpos);
			//return sb.ToString();
		}

		protected StringParser(StringParser other, ParserCloneArgs args)
			: base(other, args)
		{
			this.BeginQuoteCharacters = other.BeginQuoteCharacters != null ? (char[])other.BeginQuoteCharacters.Clone() : null;
			this.EndQuoteCharacters = other.EndQuoteCharacters != null ? (char[])other.EndQuoteCharacters.Clone() : null;
			this.AllowDoubleQuote = other.AllowDoubleQuote;
			this.AllowEscapeCharacters = other.AllowEscapeCharacters;
			this.AllowNonQuoted = other.AllowNonQuoted;
			this.NonQuotedLetter = args.Clone(other.NonQuotedLetter);
		}

		public StringParser()
		{
			NonQuotedLetter = Terminals.LetterOrDigit;
			QuoteCharacters = "\"\'".ToCharArray();
		}

		protected override int InnerParse(ParseArgs args)
		{
			var scanner = args.Scanner;
			var pos = scanner.Position;

			if (quoteCharString != null) // AllowQuoted
			{
				var ch = scanner.ReadChar();
				if (ch == -1)
					return -1;

				var quoteIndex = quoteCharString.IndexOf((char)ch);
				if (quoteIndex >= 0)
				{
					var quote = (int)endQuoteCharString[quoteIndex];
                  beg:
					ch = scanner.ReadChar();
					if (ch != quote)
					{
						if (ch == -1)
							goto end;

						if (!AllowEscapeCharacters || ch != '\\')
							goto beg;
						// found escape character, read it
						ch = scanner.ReadChar();
						if (ch == -1)
							goto end;
						goto beg;
					}

					// reached quote, check for double quote
					if (!AllowDoubleQuote || scanner.Peek() != quote)
						return scanner.Position - pos;

					// double quote found, read it
					scanner.ReadChar();
					goto beg;
				}
			end:
				scanner.Position = pos;
			}

			if (!AllowNonQuoted || NonQuotedLetter == null)
				return -1;

			var length = 0;
			var m = NonQuotedLetter.Parse(args);
			while (m > 0)
			{
				length += m;
				m = NonQuotedLetter.Parse(args);
			}
			if (length > 0)
				return length;

			return -1;
		}

		public override Parser Clone(ParserCloneArgs args)
		{
			return new StringParser(this, args);
		}
	}
}