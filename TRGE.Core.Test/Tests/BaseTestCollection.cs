﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TRGE.Core.Test
{
    public class BaseTestCollection : AbstractTestCollection
    {
        protected string _testOutputPath;

        [ClassInitialize]
        protected override void Setup()
        {
            _testOutputPath = @"scripts\SCRIPT_OUTPUT.dat";
        }

        [ClassCleanup]
        protected override void TearDown()
        {
            File.Delete(_testOutputPath);
        }

        internal TR23Script SaveAndReload(TR23Script script)
        {
            File.WriteAllBytes(_testOutputPath, script.Serialise());
            return TRScriptFactory.OpenScript(_testOutputPath) as TR23Script;
        }
    }
}