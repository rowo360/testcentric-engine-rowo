// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric Engine contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using System;
using System.Xml;

namespace TestCentric.Engine
{
    /// <summary>
    /// Abstract base for all test filters. A filter is represented
    /// by an XmlNode with &lt;filter&gt; as its topmost element.
    /// In the console runner, filters serve only to carry this
    /// XML representation, as all filtering is done by the engine.
    /// </summary>
#if !NETSTANDARD1_6
    [Serializable]
#endif
    public class TestFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestFilter"/> class.
        /// </summary>
        /// <param name="xmlText">The XML text that specifies the filter.</param>
        public TestFilter(string xmlText)
        {
            Text = xmlText;
        }

        /// <summary>
        /// The empty filter - one that always passes.
        /// </summary>
        public static readonly TestFilter Empty = new TestFilter("<filter/>");

        /// <summary>
        /// Gets the XML representation of this filter as a string.
        /// </summary>
        public string Text { get; private set; }
    }
}
