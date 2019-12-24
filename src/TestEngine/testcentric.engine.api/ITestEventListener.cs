// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric Engine contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using TestCentric.Engine.Extensibility;

namespace TestCentric.Engine
{
    /// <summary>
    /// The ITestListener interface is used to receive notices of significant
    /// events while a test is running. Its single method accepts an Xml string,
    /// which may represent any event generated by the test framework, the driver
    /// or any of the runners internal to the engine. Use of Xml means that
    /// any driver and framework may add additional events and the engine will
    /// simply pass them on through this interface.
    /// </summary>
    [TypeExtensionPoint(
        Description = "Allows an extension to process progress reports and other events from the test.")]
    public interface ITestEventListener
    {
        /// <summary>
        /// Handle a progress report or other event.
        /// </summary>
        /// <param name="report">An XML progress report.</param>
        void OnTestEvent(string report);
    }
}
