#region usings

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

#endregion

namespace ExportSrc
{
    [Serializable]
    public class Filter : IEquatable<Filter>
    {
        private bool _enabled;

        [XmlIgnore]
        private Regex _regex;

        private string _text;

        public Filter(
            string text,
            FilterType filterType,
            bool applyToFileName,
            bool applyToPath,
            bool applyToDirectory,
            bool applyToFile)
        {
            this.Text = text;
            this.ApplyToPath = applyToPath;
            this.ApplyToFileName = applyToFileName;
            this.ApplyToDirectory = applyToDirectory;
            this.ApplyToFile = applyToFile;
            this.FilterType = filterType;
            this.Enabled = true;
        }

        public Filter(string text, FilterType filterType, bool matchDirectory, bool matchFile)
            : this(text, filterType, true, false, matchDirectory, matchFile)
        {
        }

        public Filter(string text, FilterType filterType)
            : this(text, filterType, true, true)
        {
        }

        public Filter()
            : this(null, FilterType.Exclude, true, true, true, true)
        {
        }

        [XmlAttribute]
        public bool ApplyToDirectory { get; set; }

        [XmlAttribute]
        public bool ApplyToFile { get; set; }

        [XmlAttribute]
        public bool ApplyToFileName { get; set; }

        [XmlAttribute]
        public bool ApplyToPath { get; set; }

        [XmlAttribute]
        [DefaultValue(false)]
        public bool CaseSensitive { get; set; }

        [XmlAttribute]
        [DefaultValue(true)]
        public bool Enabled
        {
            get => !string.IsNullOrEmpty(this.Text) && this._enabled;
            set => this._enabled = value;
        }

        [XmlAttribute]
        [DefaultValue(typeof(FilterExpressionType), "Globbing")]
        public FilterExpressionType ExpressionType { get; set; }

        [XmlAttribute]
        [DefaultValue(typeof(FilterType), "Exclude")]
        public FilterType FilterType { get; set; }

        [XmlText]
        public string Text
        {
            get => this._text;
            set
            {
                if (this._text == value)
                    return;

                this._text = value;
                this._regex = null;
            }
        }

        public static bool operator ==(Filter left, Filter right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Filter left, Filter right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Filter other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.FilterType, this.FilterType) && other.ApplyToFileName.Equals(this.ApplyToFileName)
                                                             && other.ApplyToPath.Equals(this.ApplyToPath)
                                                             && other.ApplyToFile.Equals(this.ApplyToFile)
                                                             && other.ApplyToDirectory.Equals(this.ApplyToDirectory)
                                                             && other.CaseSensitive.Equals(this.CaseSensitive)
                                                             && Equals(other._text, this._text);
        }

        /// <summary>
        ///     Determines whether the specified <see cref="T:System.Object" /> is equal to the current
        ///     <see cref="T:System.Object" />.
        /// </summary>
        /// <returns>
        ///     true if the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />;
        ///     otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />. </param>
        /// <exception cref="T:System.NullReferenceException">The <paramref name="obj" /> parameter is null.</exception>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Filter)) return false;
            return this.Equals((Filter)obj);
        }

        /// <summary>
        ///     Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        ///     A hash code for the current <see cref="T:System.Object" />.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                var result = this.FilterType.GetHashCode();
                result = (result * 397) ^ this.ApplyToFileName.GetHashCode();
                result = (result * 397) ^ this.ApplyToPath.GetHashCode();
                result = (result * 397) ^ this.ApplyToFile.GetHashCode();
                result = (result * 397) ^ this.ApplyToDirectory.GetHashCode();
                result = (result * 397) ^ this.CaseSensitive.GetHashCode();
                result = (result * 397) ^ (this._text != null ? this._text.GetHashCode() : 0);
                return result;
            }
        }

        public bool Match(string fullPath, string relativePath, string name)
        {
            var isFile = File.Exists(fullPath);
            var isDirectory = Directory.Exists(fullPath);

            if (!isFile && !isDirectory)
                return false;

            if (this.ApplyToFile && !isFile && !this.ApplyToDirectory)
                return false;

            if (this.ApplyToDirectory && !isDirectory && !this.ApplyToFile)
                return false;

            if (this.ApplyToFileName && this.Match(name))
                return true;

            if (this.ApplyToPath && this.Match(relativePath))
                return true;

            return false;
        }

        public override string ToString()
        {
            return string.Format(
                "FilterType: {1}, Text: {0}, CaseSensitive: {2}",
                this._text,
                this.FilterType,
                this.CaseSensitive);
        }

        private void BuildRegex()
        {
            if (this._regex == null)
            {
                var options = RegexOptions.Singleline | RegexOptions.Compiled;
                if (!this.CaseSensitive) options |= RegexOptions.IgnoreCase;

                switch (this.ExpressionType)
                {
                    case FilterExpressionType.Regex:
                        this._regex = new Regex(this.Text, options);
                        break;
                    case FilterExpressionType.Globbing:
                    default:
                        var escape = Regex.Escape(this.Text);
                        this._regex = new Regex(
                            "^" + escape.Replace(@"\*", ".*").Replace(@"\|", "|").Replace(@"\?", ".") + "$",
                            options);
                        break;
                }
            }
        }

        private bool Match(string name)
        {
            this.BuildRegex();
            return this._regex.IsMatch(name);
        }
    }
}