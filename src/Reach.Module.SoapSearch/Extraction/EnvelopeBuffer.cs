using System.Text;

namespace Reach.Module.SoapSearch.Extraction;

// Finds the end of the first XML root without interpreting or rewriting its text.
// XML validation happens after capture. State survives log lines and quoted '>'.
internal sealed class EnvelopeBuffer
{
    private readonly StringBuilder _text = new();
    private Mode _mode;
    private int _tagStart;
    private int _depth;
    private char _quote;
    private bool _started;

    public bool Append(string value)
    {
        foreach (var character in value)
        {
            if (!_started)
            {
                if (character != '<')
                {
                    continue;
                }

                _started = true;
            }

            _text.Append(character);
            switch (_mode)
            {
                case Mode.Text:
                    if (character == '<')
                    {
                        _tagStart = _text.Length - 1;
                        _mode = Mode.Tag;
                    }
                    break;
                case Mode.Tag:
                    var length = _text.Length - _tagStart;
                    if (length == 2 && character == '?')
                    {
                        _mode = Mode.Instruction;
                    }
                    else if (length == 4 && EndsWith("<!--"))
                    {
                        _mode = Mode.Comment;
                    }
                    else if (length == 9 && EndsWith("<![CDATA["))
                    {
                        _mode = Mode.Cdata;
                    }
                    else if (_quote != '\0')
                    {
                        if (character == _quote)
                        {
                            _quote = '\0';
                        }
                    }
                    else if (character is '\'' or '"')
                    {
                        _quote = character;
                    }
                    else if (character == '>')
                    {
                        _mode = Mode.Text;
                        if (_text[_tagStart + 1] == '!')
                        {
                            break;
                        }

                        if (_text[_tagStart + 1] == '/')
                        {
                            _depth--;
                        }
                        else if (_text[^2] != '/')
                        {
                            _depth++;
                        }

                        if (_depth <= 0)
                        {
                            return true;
                        }
                    }
                    break;
                case Mode.Comment:
                    if (EndsWith("-->"))
                    {
                        _mode = Mode.Text;
                    }
                    break;
                case Mode.Cdata:
                    if (EndsWith("]]>"))
                    {
                        _mode = Mode.Text;
                    }
                    break;
                case Mode.Instruction:
                    if (EndsWith("?>"))
                    {
                        _mode = Mode.Text;
                    }
                    break;
            }
        }

        return false;
    }

    public override string ToString() => _text.ToString();

    private bool EndsWith(string value)
    {
        if (_text.Length < value.Length)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (_text[_text.Length - value.Length + i] != value[i])
            {
                return false;
            }
        }

        return true;
    }

    private enum Mode
    {
        Text,
        Tag,
        Comment,
        Cdata,
        Instruction,
    }
}
