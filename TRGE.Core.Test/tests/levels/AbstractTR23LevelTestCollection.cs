﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace TRGE.Core.Test
{
    public abstract class AbstractTR23LevelTestCollection : BaseTestCollection
    {
        protected abstract int ScriptFileIndex { get; }
        List<AbstractTRLevel> _expectedLevels;
        protected abstract string[] LevelNames { get; }
        protected abstract string[] LevelFileNames { get; }

        protected void InitialiseLevels()
        {
            _expectedLevels = new List<AbstractTRLevel>();
            for (int i = 0; i < LevelNames.Length; i++)
            {
                _expectedLevels.Add(new TR23Level
                {
                    Name = LevelNames[i],
                    LevelFile = LevelFileNames[i]
                });
            }
        }

        [TestMethod]
        protected virtual void TestLoadLevels()
        {
            InitialiseLevels();
            TR23ScriptManager sm = TRGameflowEditor.Instance.GetScriptManager(_validScripts[ScriptFileIndex]) as TR23ScriptManager;
            try
            {
                CollectionAssert.AreEqual(sm.LevelManager.Levels, _expectedLevels);
            }
            finally
            {
                TRGameflowEditor.Instance.CloseScriptManager(sm);
            }
        }

        [TestMethod]
        protected void TestRandomiseLevels()
        {
            InitialiseLevels();
            RandomGenerator rng = new RandomGenerator(RandomGenerator.Type.Date);
            _expectedLevels.Randomise(rng.Create());

            TR23ScriptManager sm = TRGameflowEditor.Instance.GetScriptManager(_validScripts[ScriptFileIndex]) as TR23ScriptManager;
            try
            {
                sm.LevelOrganisation = Organisation.Random;
                sm.LevelRNG = rng;
                sm.RandomiseLevels();
                List<AbstractTRLevel> levels = sm.LevelManager.Levels;
                CollectionAssert.AreEqual(levels, _expectedLevels);

                for (int i = 0; i < levels.Count; i++)
                {
                    if (i == levels.Count - 1)
                    {
                        Assert.IsTrue(levels[i].IsFinalLevel, "Level at final index is not marked as the final level");
                    }
                    else
                    {
                        Assert.IsFalse(levels[i].IsFinalLevel, string.Format("Level at index {0} is marked as the final level", i));
                    }
                }
            }
            finally
            {
                TRGameflowEditor.Instance.CloseScriptManager(sm);
            }
        }

        [TestMethod]
        protected void TestReorganiseLevels()
        {
            InitialiseLevels();
            _expectedLevels.Reverse();
            TR23ScriptManager sm = TRGameflowEditor.Instance.GetScriptManager(_validScripts[ScriptFileIndex]) as TR23ScriptManager;
            try
            {
                List<Tuple<string, string>> levelSequencingData = sm.LevelSequencing;
                levelSequencingData.Reverse();
                sm.LevelSequencing = levelSequencingData;
                CollectionAssert.AreEqual(sm.LevelManager.Levels, _expectedLevels);
            }
            finally
            {
                TRGameflowEditor.Instance.CloseScriptManager(sm);
            }
        }
    }
}